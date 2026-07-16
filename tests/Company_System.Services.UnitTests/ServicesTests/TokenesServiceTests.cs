using System.Net;
using AutoFixture;
using FluentAssertions;
using HR_System.Core.common;
using HR_System.Core.Constraints;
using HR_System.Core.Domain.Entities;
using HR_System.Core.Domain.Identity;
using HR_System.Core.DTO.Token;
using HR_System.Core.Enums;
using HR_System.Core.Interfaces.ServiceContracts;
using HR_System.Infrastructure.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;

namespace TestProject1.ServicesTests;

public class TokensServiceTests
{
    private readonly TokensService _tokensService;
    private readonly Mock<IAccessTokenService> _accessTokenServiceMock;
    private readonly Mock<IRefreshTokenService> _refreshTokenServiceMock;
    private readonly Mock<ICookiesServices> _cookiesServicesMock;
    private readonly Mock<UserManager<ApplicationUser>> _userManagerMock;
    private readonly IFixture _fixture;

    private const string RefreshTokenCookieKey = "refreshToken";

    public TokensServiceTests()
    {
        _fixture = new Fixture();
        _fixture.Behaviors.OfType<ThrowingRecursionBehavior>().ToList().ForEach(b => _fixture.Behaviors.Remove(b));
        _fixture.Behaviors.Add(new OmitOnRecursionBehavior());

        _accessTokenServiceMock = new Mock<IAccessTokenService>();
        _refreshTokenServiceMock = new Mock<IRefreshTokenService>();
        _cookiesServicesMock = new Mock<ICookiesServices>();

        var storeMock = new Mock<IUserStore<ApplicationUser>>();
        _userManagerMock = new Mock<UserManager<ApplicationUser>>(
            storeMock.Object, null!, null!, null!, null!, null!, null!, null!, null!);

        var cookieKeys = Options.Create(new CookieKeys { RefreshToken = RefreshTokenCookieKey });

        _tokensService = new TokensService(
            _accessTokenServiceMock.Object,
            _refreshTokenServiceMock.Object,
            _cookiesServicesMock.Object,
            _userManagerMock.Object,
            cookieKeys,
            NullLogger<TokensService>.Instance);
    }

    [Fact]
    public async Task GenerateNewTokensAsync_ShouldReturnFailure_WhenAccessTokenFails()
    {
        var user = new ApplicationUser { Id = Guid.NewGuid(), FullName = "test full name", Position = PositionsEnum.unknown};
        _accessTokenServiceMock.Setup(s => s.GenerateAccessTokenAsync(user))
            .ReturnsAsync(Result<string>.Failure("Access Token Error", HttpStatusCode.BadRequest));

        var result = await _tokensService.GenerateNewTokensAsync(user);

        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Be("Access Token Error");
    }

    [Fact]
    public async Task RegenerateTokensAsync_ShouldReturnFailure_WhenCookieIsMissing()
    {
        _cookiesServicesMock.Setup(c => c.Get(RefreshTokenCookieKey))
            .Returns(Result<string>.Failure("No cookie", HttpStatusCode.BadRequest));

        var result = await _tokensService.RegenerateTokensAsync();

        result.IsSuccess.Should().BeFalse();
        _refreshTokenServiceMock.Verify(s => s.ConsumeRefreshTokenAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task UpdateUserTokensAsync_ShouldReturnSuccess_WhenFullCycleCompletes()
    {
        // Setup successful cookie retrieval
        var cookieToken = "existing-token";
        _cookiesServicesMock.Setup(c => c.Get(RefreshTokenCookieKey)).Returns(Result<string>.Success(cookieToken));

        // Setup successful token consumption
        var user = new ApplicationUser { Id = Guid.NewGuid(), FullName = "test full name" , Position = PositionsEnum.unknown};
        var consumedToken = new RefreshToken { UserId = user.Id,  Token = cookieToken, Expires = DateTime.UtcNow.AddMinutes(5) };
        _refreshTokenServiceMock.Setup(s => s.ConsumeRefreshTokenAsync(cookieToken, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<RefreshToken>.Success(consumedToken));

        // Setup successful user search
        _userManagerMock.Setup(m => m.FindByIdAsync(user.Id.ToString())).ReturnsAsync(user);

        // Setup successful token generations
        _accessTokenServiceMock.Setup(s => s.GenerateAccessTokenAsync(user))
            .ReturnsAsync(Result<string>.Success("new-access-token"));
        _refreshTokenServiceMock.Setup(s => s.GenerateRefreshTokenAsync(user.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<string>.Success("new-refresh-token"));

        // Setup successful cookie write
        _cookiesServicesMock.Setup(c => c.AddTokens(It.IsAny<AccessAndRefreshTokenDTO>()))
            .Returns(Result.Success());

        var result = await _tokensService.UpdateUserTokensAsync();

        result.IsSuccess.Should().BeTrue();
        result.Value!.AccessToken.Should().Be("new-access-token");
        result.Value.RefreshToken.Should().Be("new-refresh-token");
    }
}