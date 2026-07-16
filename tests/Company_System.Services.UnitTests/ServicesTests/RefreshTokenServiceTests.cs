using System.Net;
using AutoFixture;
using FluentAssertions;
using HR_System.Core.Domain.Entities;
using HR_System.Core.Interfaces.RepositoryContracts;
using HR_System.Infrastructure.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit.Abstractions;

namespace TestProject1.ServicesTests;

public class RefreshTokenServiceTests
{
    private readonly RefreshTokenService _refreshTokenService;
    private readonly Mock<IRefreshTokensRepository> _refreshTokensRepositoryMock;
    private readonly IFixture _fixture;
    private readonly ITestOutputHelper _output;

    public RefreshTokenServiceTests(ITestOutputHelper output)
    {
        _output = output;
        
        _fixture = new Fixture();
        _fixture.Behaviors.OfType<ThrowingRecursionBehavior>().ToList().ForEach(b => _fixture.Behaviors.Remove(b));
        _fixture.Behaviors.Add(new OmitOnRecursionBehavior());

        _refreshTokensRepositoryMock = new Mock<IRefreshTokensRepository>();

        var inMemorySettings = new Dictionary<string, string?> { { "Jwt:RefreshTokenLifeTime", "10080" } };
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(inMemorySettings).Build();

        var loggerMock = new Mock<ILogger<RefreshTokenService>>();

        _refreshTokenService = new RefreshTokenService(
            _refreshTokensRepositoryMock.Object, configuration, loggerMock.Object);
    }

    private RefreshToken CreateRefreshToken(bool isResolved = false)
    {
        return _fixture.Build<RefreshToken>()
            .With(r => r.Expires, DateTime.UtcNow.AddDays(isResolved ? -1 : 1))
            .Without(r => r.User)
            .Create();
    }

    [Fact]
    public async Task GenerateRefreshTokenAsync_ShouldAddRefreshTokenAndSaveChanges()
    {
        // Arrange
        var userId = Guid.NewGuid();

        _refreshTokensRepositoryMock.Setup(t => t.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        
        _output.WriteLine($"userId: {userId}");
        
        // Act
        var result = await _refreshTokenService.GenerateRefreshTokenAsync(userId);
        _output.WriteLine($"result: {result.ToString()}");
        
        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNullOrWhiteSpace();
        _refreshTokensRepositoryMock.Verify(r => r.AddAsync(It.Is<RefreshToken>(t => t.UserId == userId), It.IsAny<CancellationToken>()), Times.Once);
        _refreshTokensRepositoryMock.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ConsumeRefreshTokenAsync_ShouldReturnFailure_WhenTokenNotFound()
    {
        _refreshTokensRepositoryMock
            .Setup(r => r.RemoveRefreshTokenByRefreshTokenString(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((RefreshToken?)null);

        var result = await _refreshTokenService.ConsumeRefreshTokenAsync("invalid-token");

        result.IsSuccess.Should().BeFalse();
        result.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task ConsumeRefreshTokenAsync_ShouldReturnFailure_WhenTokenIsResolved()
    {
        var resolvedToken = CreateRefreshToken(isResolved: true);
        _refreshTokensRepositoryMock
            .Setup(r => r.RemoveRefreshTokenByRefreshTokenString(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(resolvedToken);

        var result = await _refreshTokenService.ConsumeRefreshTokenAsync(resolvedToken.Token);

        result.IsSuccess.Should().BeFalse();
    }

    [Fact]
    public async Task ConsumeRefreshTokenAsync_ShouldReturnSuccess_WhenTokenIsValid()
    {
        var validToken = CreateRefreshToken(isResolved: false);
        _refreshTokensRepositoryMock
            .Setup(r => r.RemoveRefreshTokenByRefreshTokenString(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(validToken);

        var result = await _refreshTokenService.ConsumeRefreshTokenAsync("valid-token");

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeEquivalentTo(validToken);
    }
}