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

public class RefreshTokensesRepositoryTests : IDisposable
{
    private readonly ITestOutputHelper _output;
    private readonly IRefreshTokensRepository _refreshTokensRepository;
    private readonly IFixture _fixture;
    private readonly ApplicationDbContext _dbContext;
    private readonly Mock<IRedisService> _redisMock;

    public RefreshTokensesRepositoryTests(ITestOutputHelper output)
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
        
        _redisMock = new Mock<IRedisService>();

        
        _redisMock
            .Setup(t => t.Get<RefreshToken>(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(null as RefreshToken);
        _redisMock
            .Setup(t => t.Set(It.IsAny<string>(), It.IsAny<object>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _refreshTokensRepository = new RefreshTokensesRepository(_dbContext, _redisMock.Object);
    }

    #region FindRefreshTokenByRefreshTokenStringAsync

    [Fact]
    public async Task FindRefreshTokenByRefreshTokenStringAsync_TokenInDb_ShouldReturnToken()
    {
        // Arrange
        var token = CreateToken();
        await SeedAsync(token);

        _output.WriteLine($"Expected Token: {token.Token}");

        // Act
        var actual = await _refreshTokensRepository.FindRefreshTokenByRefreshTokenStringAsync(token.Token);
        _output.WriteLine($"Actual Token: {actual?.Token}");

        // Assert
        actual.Should().NotBeNull();
        actual.Should().BeEquivalentTo(token);

        // Verify cache was checked then set
        _redisMock.Verify(t =>
            t.Get<RefreshToken>(token.Token, It.IsAny<CancellationToken>()), Times.Once);
        _redisMock.Verify(t =>
            t.Set(token.Token, It.IsAny<object>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task FindRefreshTokenByRefreshTokenStringAsync_TokenInCache_ShouldReturnCachedToken()
    {
        // Arrange
        var token = CreateToken();

        // Override default — cache returns the token
        _redisMock
            .Setup(t => t.Get<RefreshToken>(token.Token, It.IsAny<CancellationToken>()))
            .ReturnsAsync(token);

        _output.WriteLine($"Expected Token: {token.Token}");

        // Act
        var actual = await _refreshTokensRepository.FindRefreshTokenByRefreshTokenStringAsync(token.Token);
        _output.WriteLine($"Actual Token: {actual?.Token}");

        // Assert
        actual.Should().NotBeNull();
        actual.Should().BeEquivalentTo(token);

        // Verify DB was never hit and cache was never set
        _redisMock.Verify(t =>
            t.Set(It.IsAny<string>(), It.IsAny<object>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task FindRefreshTokenByRefreshTokenStringAsync_TokenNotFound_ShouldReturnNull()
    {
        // Arrange
        var nonExistentToken = _fixture.Create<string>();
        _output.WriteLine($"Searching for: {nonExistentToken}");

        // Act
        var actual = await _refreshTokensRepository.FindRefreshTokenByRefreshTokenStringAsync(nonExistentToken);
        _output.WriteLine($"Actual: {actual?.ToString() ?? "null"}");

        // Assert
        actual.Should().BeNull();
    }

    #endregion

    #region AddAsync

    [Fact]
    public async Task AddAsync_ValidToken_ShouldPersistAfterSave()
    {
        // Arrange
        var token = CreateToken();
        _output.WriteLine($"Adding Token: {token.Token}");

        // Act
        _refreshTokensRepository.AddAsync(token);
        await _refreshTokensRepository.SaveChangesAsync();

        // Assert
        var actual = await _refreshTokensRepository
            .FindRefreshTokenByRefreshTokenStringAsync(token.Token);

        _output.WriteLine($"Actual Token: {actual?.Token}");
        actual.Should().NotBeNull();
        actual.Should().BeEquivalentTo(token);
    }

    [Fact]
    public async Task AddAsync_WithoutSaving_ShouldNotPersist()
    {
        // Arrange
        var token = CreateToken();
        _output.WriteLine($"Adding Token without saving: {token.Token}");

        // Act — skip SaveChangesAsync intentionally
        _refreshTokensRepository.AddAsync(token);

        // Assert
        var actual = await _refreshTokensRepository
            .FindRefreshTokenByRefreshTokenStringAsync(token.Token);

        _output.WriteLine($"Actual: {actual?.ToString() ?? "null"}");
        actual.Should().BeNull();
    }

    #endregion

    #region SaveChangesAsync

    [Fact]
    public async Task SaveChangesAsync_WithPendingChanges_ShouldReturnTrue()
    {
        // Arrange
        var token = CreateToken();
        _refreshTokensRepository.AddAsync(token);
        _output.WriteLine($"Added Token: {token.Token}");

        // Act
        var actual = await _refreshTokensRepository.SaveChangesAsync();
        _output.WriteLine($"Expected: true | Actual: {actual}");

        // Assert
        actual.Should().BeTrue();
    }

    [Fact]
    public async Task SaveChangesAsync_WithNoChanges_ShouldReturnFalse()
    {
        // Arrange
        _output.WriteLine("No changes made");

        // Act
        var actual = await _refreshTokensRepository.SaveChangesAsync();
        _output.WriteLine($"Expected: false | Actual: {actual}");

        // Assert
        actual.Should().BeFalse();
    }

    #endregion

    #region Helpers

    private RefreshToken CreateToken(bool? expired = null) =>
        _fixture.Build<RefreshToken>()
            .With(t => t.Expires, expired == true
                ? DateTime.UtcNow.AddMinutes(-10)  // already expired
                : DateTime.UtcNow.AddMinutes(60))  // valid
            .Without(t => t.User)
            .Create();

    private List<RefreshToken> CreateMany(int count, bool? expired = null) =>
        Enumerable.Range(0, count)
            .Select(_ => CreateToken(expired))
            .ToList();

    private async Task SeedAsync(params RefreshToken[] tokens)
    {
        await _dbContext.RefreshTokens.AddRangeAsync(tokens);
        await _dbContext.SaveChangesAsync();
    }

    public void Dispose() => _dbContext.Dispose();

    #endregion
}