using System.Net;
using AutoFixture;
using FluentAssertions;
using HR_System.Core.Abstraction.RepositoryContracts;
using HR_System.Core.Domain.Entities;
using HR_System.Infrastructure.Services;
using Microsoft.Extensions.Configuration;
using Moq;
using Xunit.Abstractions;

namespace HR_System.Core.UnitTests.ServicesTests;

public class RefreshTokenServiceTests : ServiceTestBase
{
    private readonly RefreshTokenService _refreshTokenService;
    private readonly Mock<IRefreshTokensRepository> _refreshTokensRepositoryMock;

    public RefreshTokenServiceTests(ITestOutputHelper output) : base(output)
    {
        _refreshTokensRepositoryMock = new Mock<IRefreshTokensRepository>();
        var inMemorySettings = new Dictionary<string, string?>
        {
            {
                "Jwt:RefreshTokenLifeTime",
                "10080"
            }
        };
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(inMemorySettings).Build();
        _refreshTokenService = new RefreshTokenService(_refreshTokensRepositoryMock.Object, Microsoft.Extensions.Options.Options.Create(new HR_System.Core.Options.JWTOptions { RefreshTokenLifeTime = 10080 }));
    }

    private RefreshToken CreateRefreshToken(bool isResolved = false)
    {
        return Fixture.Build<RefreshToken>()
            .With(r => r.Expires, DateTime.UtcNow.AddDays(isResolved ? -1 : 1))
            .Without(r => r.User)
            .Create();
    }

    // ---------- GenerateRefreshTokenAsync ----------
    #region GenerateRefreshTokenAsync
    [Fact]
    public async Task GenerateRefreshTokenAsync_ShouldAddRefreshTokenAndSaveChanges()
    {
        // Arrange
        var userId = Guid.NewGuid();
        _refreshTokensRepositoryMock.Setup(t => t.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        Output.WriteLine($"userId: {userId}");
        LogArrange(nameof(userId), userId);

        // Act
        var result = await _refreshTokenService.GenerateRefreshTokenAsync(userId);
        Output.WriteLine($"result: {result.ToString()}");
        LogAct(result);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNullOrWhiteSpace();
        _refreshTokensRepositoryMock.Verify(
            r => r.AddAsync(It.Is<RefreshToken>(t => t.UserId == userId), It.IsAny<CancellationToken>()),
            Times.Once
        );
        _refreshTokensRepositoryMock.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    #endregion
    // ---------- ConsumeRefreshTokenAsync ----------
    #region ConsumeRefreshTokenAsync
    [Fact]
    public async Task ConsumeRefreshTokenAsync_ShouldReturnFailure_WhenTokenNotFound()
    {
        // Arrange
        LogArrange("Scenario", nameof(ConsumeRefreshTokenAsync_ShouldReturnFailure_WhenTokenNotFound));

        _refreshTokensRepositoryMock.Setup(r => r.GetByTokenAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((RefreshToken? )null);

        // Act
        var result = await _refreshTokenService.ConsumeRefreshTokenAsync("invalid-token");
        LogAct(result);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task ConsumeRefreshTokenAsync_ShouldReturnFailure_WhenTokenIsResolved()
    {
        // Arrange
        LogArrange("Scenario", nameof(ConsumeRefreshTokenAsync_ShouldReturnFailure_WhenTokenIsResolved));

        var resolvedToken = CreateRefreshToken(isResolved: true);
        _refreshTokensRepositoryMock.Setup(r => r.GetByTokenAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(resolvedToken);

        // Act
        var result = await _refreshTokenService.ConsumeRefreshTokenAsync(resolvedToken.Token);
        LogAct(result);

        // Assert
        result.IsSuccess.Should().BeFalse();
    }

    [Fact]
    public async Task ConsumeRefreshTokenAsync_ShouldReturnSuccess_WhenTokenIsValid()
    {
        // Arrange
        LogArrange("Scenario", nameof(ConsumeRefreshTokenAsync_ShouldReturnSuccess_WhenTokenIsValid));

        var validToken = CreateRefreshToken(isResolved: false);
        _refreshTokensRepositoryMock.Setup(r => r.GetByTokenAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(validToken);
        _refreshTokensRepositoryMock.Setup(repository => repository.RemoveAsync(validToken, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        // Act
        var result = await _refreshTokenService.ConsumeRefreshTokenAsync("valid-token");
        LogAct(result);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeEquivalentTo(validToken);
    }
    #endregion
}
