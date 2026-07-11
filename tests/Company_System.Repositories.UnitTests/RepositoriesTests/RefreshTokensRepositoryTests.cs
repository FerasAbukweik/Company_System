using AutoFixture;
using FluentAssertions;
using HR_System.Core.Domain.Entities;
using HR_System.Core.Interfaces.RepositoryContracts;
using HR_System.Core.Interfaces.ServiceContracts;
using HR_System.Infrastructure;
using HR_System.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using Moq;
using Xunit.Abstractions;

namespace TestProject1.RepositoriesTests;

public class RefreshTokensRepositoryTests : IDisposable
{
    private readonly IRefreshTokensRepository _refreshTokensRepository;
    private readonly ApplicationDbContext _dbContext;
    private readonly Mock<IRedisService> _cacheMock;
    private readonly ITestOutputHelper _output;
    private readonly IFixture _fixture;

    public RefreshTokensRepositoryTests(ITestOutputHelper output)
    {
        _output = output;

        _fixture = new Fixture();
        _fixture.Behaviors.OfType<ThrowingRecursionBehavior>().ToList()
            .ForEach(b => _fixture.Behaviors.Remove(b));
        _fixture.Behaviors.Add(new OmitOnRecursionBehavior());

        var dbOptions = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        _dbContext = new ApplicationDbContext(dbOptions);

        // IRedisService is an external dependency (cache), not part of this
        // repository's own logic, so it's mocked rather than exercised for real.
        _cacheMock = new Mock<IRedisService>();

        _refreshTokensRepository = new RefreshTokensRepository(_dbContext, _cacheMock.Object);
    }

    private RefreshToken CreateToken(Guid userId, DateTime expires, string? tokenValue = null)
    {
        return _fixture.Build<RefreshToken>()
            .With(t => t.UserId, userId)
            .With(t => t.Expires, expires)
            .With(t => t.Token, tokenValue ?? Guid.NewGuid().ToString("N"))
            .Without(t => t.User)
            .Create();
    }

    #region AddAsync

    [Fact]
    public void AddAsync_ShouldTrackEntityAsAdded()
    {
        // Arrange
        var token = CreateToken(Guid.NewGuid(), DateTime.UtcNow.AddDays(1));

        // Act
        _refreshTokensRepository.AddAsync(token);

        // Assert
        _dbContext.Entry(token).State.Should().Be(EntityState.Added);
        _dbContext.RefreshTokens.Local.Should().Contain(token);
    }

    [Fact]
    public void AddAsync_ShouldNotPersistToDatabase_BeforeSaveChangesIsCalled()
    {
        // Arrange
        var token = CreateToken(Guid.NewGuid(), DateTime.UtcNow.AddDays(1));

        // Act
        _refreshTokensRepository.AddAsync(token);

        // Assert
        _dbContext.RefreshTokens.AsNoTracking().Any(t => t.Id == token.Id).Should().BeFalse();
    }

    #endregion

    #region RemoveExpiredRefreshTokensAsync

    // Not unit-testable as written: this method calls ExecuteDeleteAsync, which is
    // not supported by EF Core's InMemory provider (it throws InvalidOperationException
    // at runtime). Covering this requires an integration test against a real relational
    // provider (e.g. SQL Server, or SQLite in-memory as a lighter-weight alternative).
    [Fact(Skip = "Uses ExecuteDeleteAsync, which is not supported by UseInMemoryDatabase. Needs an integration test against a real relational provider.")]
    public Task RemoveExpiredRefreshTokensAsync_RequiresRelationalProvider()
    {
        return Task.CompletedTask;
    }

    #endregion

    #region FindRefreshTokenByRefreshTokenStringAsync

    [Fact]
    public async Task FindRefreshTokenByRefreshTokenStringAsync_ShouldReturnCachedToken_WhenPresentInCache()
    {
        // Arrange
        var tokenString = "cached-token";
        var cachedToken = CreateToken(Guid.NewGuid(), DateTime.UtcNow.AddDays(1), tokenString);

        _cacheMock
            .Setup(c => c.GetAsync<RefreshToken>(tokenString, It.IsAny<CancellationToken>()))
            .ReturnsAsync(cachedToken);

        // Act
        var result = await _refreshTokensRepository.FindRefreshTokenByRefreshTokenStringAsync(tokenString);

        // Assert
        result.Should().Be(cachedToken);
        _cacheMock.Verify(
            c => c.SetAsync(It.IsAny<string>(), It.IsAny<RefreshToken?>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task FindRefreshTokenByRefreshTokenStringAsync_ShouldQueryDbAndCacheResult_WhenNotInCache()
    {
        // Arrange
        var tokenString = "db-token";
        var dbToken = CreateToken(Guid.NewGuid(), DateTime.UtcNow.AddDays(1), tokenString);
        _dbContext.RefreshTokens.Add(dbToken);
        await _dbContext.SaveChangesAsync();

        _cacheMock
            .Setup(c => c.GetAsync<RefreshToken>(tokenString, It.IsAny<CancellationToken>()))
            .ReturnsAsync((RefreshToken?)null);

        // Act
        var result = await _refreshTokensRepository.FindRefreshTokenByRefreshTokenStringAsync(tokenString);

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(dbToken.Id);

        _cacheMock.Verify(
            c => c.SetAsync(tokenString, It.Is<RefreshToken?>(t => t != null && t.Id == dbToken.Id), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task FindRefreshTokenByRefreshTokenStringAsync_ShouldReturnNull_WhenTokenNotInCacheOrDb()
    {
        // Arrange
        var tokenString = "missing-token";

        _cacheMock
            .Setup(c => c.GetAsync<RefreshToken>(tokenString, It.IsAny<CancellationToken>()))
            .ReturnsAsync((RefreshToken?)null);

        // Act
        var result = await _refreshTokensRepository.FindRefreshTokenByRefreshTokenStringAsync(tokenString);

        // Assert
        result.Should().BeNull();
    }

    #endregion

    #region RemoveRefreshTokenByRefreshTokenString

    [Fact]
    public async Task RemoveRefreshTokenByRefreshTokenString_ShouldRemoveTokenAndCacheEntry_WhenTokenExists()
    {
        // Arrange
        var tokenString = "to-remove";
        var dbToken = CreateToken(Guid.NewGuid(), DateTime.UtcNow.AddDays(1), tokenString);
        _dbContext.RefreshTokens.Add(dbToken);
        await _dbContext.SaveChangesAsync();

        _cacheMock
            .Setup(c => c.GetAsync<RefreshToken>(tokenString, It.IsAny<CancellationToken>()))
            .ReturnsAsync((RefreshToken?)null);

        // Act
        var result = await _refreshTokensRepository.RemoveRefreshTokenByRefreshTokenString(tokenString);

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(dbToken.Id);
        _dbContext.Entry(dbToken).State.Should().Be(EntityState.Deleted);

        _cacheMock.Verify(c => c.RemoveAsync(tokenString, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task RemoveRefreshTokenByRefreshTokenString_ShouldReturnNull_WhenTokenDoesNotExist()
    {
        // Arrange
        var tokenString = "nonexistent-token";

        _cacheMock
            .Setup(c => c.GetAsync<RefreshToken>(tokenString, It.IsAny<CancellationToken>()))
            .ReturnsAsync((RefreshToken?)null);

        // Act
        var result = await _refreshTokensRepository.RemoveRefreshTokenByRefreshTokenString(tokenString);

        // Assert
        result.Should().BeNull();
        _cacheMock.Verify(c => c.RemoveAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    #endregion

    #region SaveChangesAsync

    [Fact]
    public async Task SaveChangesAsync_ShouldReturnTrue_WhenThereArePendingChanges()
    {
        // Arrange
        _dbContext.RefreshTokens.Add(CreateToken(Guid.NewGuid(), DateTime.UtcNow.AddDays(1)));

        // Act
        var result = await _refreshTokensRepository.SaveChangesAsync();

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task SaveChangesAsync_ShouldReturnFalse_WhenThereAreNoPendingChanges()
    {
        // Act
        var result = await _refreshTokensRepository.SaveChangesAsync();

        // Assert
        result.Should().BeFalse();
    }

    #endregion

    public void Dispose() => _dbContext.Dispose();
}