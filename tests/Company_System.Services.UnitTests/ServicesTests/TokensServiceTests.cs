using HR_System.Core.Abstraction.RepositoryContracts;
using System.Net;
using AutoFixture;
using FluentAssertions;
using HR_System.Core.Abstraction.ServiceContracts;
using HR_System.Core.Constraints;
using HR_System.Core.Domain.Entities;
using HR_System.Core.Domain.Identity;
using HR_System.Core.DTO.Token;
using HR_System.Core.Enums;
using HR_System.Core.Result;
using HR_System.Infrastructure.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using Moq;

namespace HR_System.Core.UnitTests.ServicesTests;

public class TokensServiceTests : ServiceTestBase
{
    private readonly TokensService _tokensService;
    private readonly Mock<IAccessTokenService> _accessTokenServiceMock;
    private readonly Mock<IRefreshTokenService> _refreshTokenServiceMock;
    private readonly Mock<ICookiesServices> _cookiesServicesMock;
    private readonly Mock<IApplicationUsersRepository> _usersRepositoryMock;
    private const string RefreshTokenCookieKey = "refreshToken";
    public TokensServiceTests(ITestOutputHelper output) : base(output)
    {
        _accessTokenServiceMock = new Mock<IAccessTokenService>();
        _refreshTokenServiceMock = new Mock<IRefreshTokenService>();
        _cookiesServicesMock = new Mock<ICookiesServices>();
        _usersRepositoryMock = new Mock<IApplicationUsersRepository>();
        var cookieKeys = Microsoft.Extensions.Options.Options.Create(new HR_System.Core.Options.CookieKeysOptions { RefreshToken = RefreshTokenCookieKey });
        _tokensService = new TokensService(_accessTokenServiceMock.Object, _refreshTokenServiceMock.Object, _cookiesServicesMock.Object, _usersRepositoryMock.Object, cookieKeys);
    }

    // ---------- GenerateNewTokensAsync ----------
    #region GenerateNewTokensAsync
    [Fact]
    public async Task GenerateNewTokensAsync_ShouldReturnFailure_WhenAccessTokenFails()
    {
        // Arrange
        LogArrange("Scenario", nameof(GenerateNewTokensAsync_ShouldReturnFailure_WhenAccessTokenFails));

        var user = new ApplicationUser
        {
            Id = Guid.NewGuid(),
            FullName = "test full name",
            Position = PositionsEnum.Employee
        };
        _accessTokenServiceMock.Setup(s => s.GenerateAccessTokenAsync(user))
            .ReturnsAsync(Result<AccessTokenResult>.Failure("Access Token Error", HttpStatusCode.BadRequest));

        // Act
        var result = await _tokensService.GenerateNewTokensAsync(user);
        LogAct(result);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Be("Access Token Error");
    }

    #endregion
    // ---------- RegenerateTokensAsync ----------
    #region RegenerateTokensAsync
    [Fact]
    public async Task RegenerateTokensAsync_ShouldReturnFailure_WhenCookieIsMissing()
    {
        // Arrange
        LogArrange("Scenario", nameof(RegenerateTokensAsync_ShouldReturnFailure_WhenCookieIsMissing));

        _cookiesServicesMock.Setup(c => c.Get(RefreshTokenCookieKey))
            .Returns(Result<string>.Failure("No cookie", HttpStatusCode.BadRequest));

        // Act
        var result = await _tokensService.RegenerateTokensAsync();
        LogAct(result);

        // Assert
        result.IsSuccess.Should().BeFalse();
        _refreshTokenServiceMock.Verify(
            s => s.ConsumeRefreshTokenAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never
        );
    }

    #endregion
    // ---------- UpdateUserTokensAsync ----------
    #region UpdateUserTokensAsync
    [Fact]
    public async Task UpdateUserTokensAsync_ShouldReturnSuccess_WhenFullCycleCompletes()
    {
        // Arrange
        LogArrange("Scenario", nameof(UpdateUserTokensAsync_ShouldReturnSuccess_WhenFullCycleCompletes));

        // Setup successful cookie retrieval
        var cookieToken = "existing-token";
        _cookiesServicesMock.Setup(c => c.Get(RefreshTokenCookieKey))
            .Returns(Result<string>.Success(cookieToken));
        // Setup successful token consumption
        var user = new ApplicationUser
        {
            Id = Guid.NewGuid(),
            FullName = "test full name",
            Position = PositionsEnum.Employee
        };
        var consumedToken = new RefreshToken
        {
            UserId = user.Id,
            Token = cookieToken,
            Expires = DateTime.UtcNow.AddMinutes(5)
        };
        _refreshTokenServiceMock.Setup(s => s.ConsumeRefreshTokenAsync(cookieToken, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<RefreshToken>.Success(consumedToken));
        // Setup successful user search
        _usersRepositoryMock.Setup(m => m.GetByIdAsync(
            user.Id,
            It.IsAny<System.Linq.Expressions.Expression<Func<ApplicationUser, object?>>[]>(),
            It.IsAny<CancellationToken>()
        ))
            .ReturnsAsync(user);
        // Setup successful token generations
        _accessTokenServiceMock.Setup(s => s.GenerateAccessTokenAsync(user))
            .ReturnsAsync(Result<AccessTokenResult>.Success(new AccessTokenResult { AccessToken = "new-access-token", Expires = DateTime.UtcNow.AddMinutes(15) }));
        _refreshTokenServiceMock.Setup(s => s.GenerateRefreshTokenAsync(user.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<string>.Success("new-refresh-token"));
        // Setup successful cookie write
        _cookiesServicesMock.Setup(c => c.AddTokens(It.IsAny<AccessAndRefreshTokenDTO>()))
            .Returns(Result.Result.Success());

        // Act
        var result = await _tokensService.UpdateUserTokensAsync();
        LogAct(result);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value!.AccessToken.Should().Be("new-access-token");
        result.Value.RefreshToken.Should().Be("new-refresh-token");
    }
    #endregion
}
