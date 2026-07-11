using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Security.Claims;
using AutoFixture;
using FluentAssertions;
using HR_System.Core.common;
using HR_System.Core.Constraints;
using HR_System.Core.Domain.Entities;
using HR_System.Core.Domain.Identity;
using HR_System.Core.DTO.Token;
using HR_System.Core.Enums;
using HR_System.Core.Interfaces.RepositoryContracts;
using HR_System.Core.Interfaces.ServiceContracts;
using HR_System.Infrastructure.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using Moq;
using Xunit.Abstractions;

namespace TestProject1.ServicesTests;

public class TokenServiceTests
{
    private readonly ITokenService _tokenService;
    private readonly Mock<ICookiesServices> _cookiesServicesMock;
    private readonly Mock<IRefreshTokensRepository> _refreshTokensRepositoryMock;
    private readonly Mock<UserManager<ApplicationUser>> _userManagerMock;
    private readonly IConfiguration _configuration;
    private readonly IOptions<CookieKeys> _cookieKeys;
    private readonly ITestOutputHelper _output;
    private readonly IFixture _fixture;

    private const string RefreshTokenCookieKey = "refreshToken";
    private const string JwtKey = "this-is-a-super-secret-testing-key-32bytes+"; // must be long enough for HmacSha256
    private const string JwtIssuer = "test-issuer";
    private const string JwtAudience = "test-audience";
    private const int AccessTokenLifeTimeMinutes = 15;
    private const int RefreshTokenLifeTimeMinutes = 60 * 24 * 7;

    public TokenServiceTests(ITestOutputHelper output)
    {
        _output = output;

        _fixture = new Fixture();
        _fixture.Behaviors.OfType<ThrowingRecursionBehavior>().ToList()
            .ForEach(b => _fixture.Behaviors.Remove(b));
        _fixture.Behaviors.Add(new OmitOnRecursionBehavior());

        _cookiesServicesMock = new Mock<ICookiesServices>();
        _refreshTokensRepositoryMock = new Mock<IRefreshTokensRepository>();

        var storeMock = new Mock<IUserStore<ApplicationUser>>();
        _userManagerMock = new Mock<UserManager<ApplicationUser>>(
            storeMock.Object, null!, null!, null!, null!, null!, null!, null!, null!);

        var inMemorySettings = new Dictionary<string, string?>
        {
            { "Jwt:Key", JwtKey },
            { "Jwt:Issuer", JwtIssuer },
            { "Jwt:Audience", JwtAudience },
            { "Jwt:AccessTokenLifeTime", AccessTokenLifeTimeMinutes.ToString() },
            { "Jwt:RefreshTokenLifeTime", RefreshTokenLifeTimeMinutes.ToString() }
        };
        _configuration = new ConfigurationBuilder().AddInMemoryCollection(inMemorySettings).Build();

        _cookieKeys = Options.Create(new CookieKeys { RefreshToken = RefreshTokenCookieKey, AccessToken = "accessToken" });

        _tokenService = new TokenService(
            _cookiesServicesMock.Object,
            _refreshTokensRepositoryMock.Object,
            _userManagerMock.Object,
            _configuration,
            _cookieKeys);
    }

    private ApplicationUser CreateValidUser(string? userName = null, string? email = null)
    {
        return _fixture.Build<ApplicationUser>()
            .With(u => u.UserName, userName ?? "john.doe")
            .With(u => u.Email, email ?? "john.doe@example.com")
            .With(u => u.Position, PositionsEnum.Employee)
            .Without(u => u.RefreshTokens)
            .Without(u => u.Tasks)
            .Without(u => u.CreatedTasks)
            .Without(u => u.Approvals)
            .Without(u => u.ToApprove)
            .Without(u => u.Activities)
            .Without(u => u.OrganizationHierarchy)
            .Without(u => u.SentMessages)
            .Without(u => u.ReceivedMessages)
            .Create();
    }

    private RefreshToken CreateRefreshToken(Guid? userId = null, DateTime? expires = null, string? token = null)
    {
        return _fixture.Build<RefreshToken>()
            .With(r => r.UserId, userId ?? Guid.NewGuid())
            .With(r => r.Expires, expires ?? DateTime.UtcNow.AddMinutes(30))
            .With(r => r.Token, token ?? _fixture.Create<string>())
            .Without(r => r.User)
            .Create();
    }

    private void SetupValidRolesForUser(ApplicationUser user, params string[] roles)
    {
        _userManagerMock.Setup(m => m.GetRolesAsync(user)).ReturnsAsync(roles);
    }

    #region GenerateAccessTokenAsync

    [Fact]
    public async Task GenerateAccessTokenAsync_ShouldReturnFailure_WhenUserNameIsMissing()
    {
        // Arrange
        var user = CreateValidUser(userName: "");

        // Act
        var result = await _tokenService.GenerateAccessTokenAsync(user);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Be("access token Cannt be Created because of missing userName or Email");
        result.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task GenerateAccessTokenAsync_ShouldReturnFailure_WhenEmailIsMissing()
    {
        // Arrange
        var user = CreateValidUser(email: "");

        // Act
        var result = await _tokenService.GenerateAccessTokenAsync(user);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Be("access token Cannt be Created because of missing userName or Email");
        result.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task GenerateAccessTokenAsync_ShouldReturnFailure_WhenUserHasNoRoles()
    {
        // Arrange
        var user = CreateValidUser();
        SetupValidRolesForUser(user); // no roles

        // Act
        var result = await _tokenService.GenerateAccessTokenAsync(user);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Be("user has no roles");
        result.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task GenerateAccessTokenAsync_ShouldReturnSuccessWithValidJwt_WhenAllDataValid()
    {
        // Arrange
        var user = CreateValidUser();
        SetupValidRolesForUser(user, "Employee");

        // Act
        var result = await _tokenService.GenerateAccessTokenAsync(user);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNullOrWhiteSpace();

        var handler = new JwtSecurityTokenHandler();
        var jwt = handler.ReadJwtToken(result.Value);

        jwt.Issuer.Should().Be(JwtIssuer);
        jwt.Audiences.Should().Contain(JwtAudience);
        jwt.Claims.Should().Contain(c => c.Type == ClaimTypes.NameIdentifier && c.Value == user.Id.ToString());
        jwt.Claims.Should().Contain(c => c.Type == ClaimTypes.Name && c.Value == user.UserName);
        jwt.Claims.Should().Contain(c => c.Type == ClaimTypes.Email && c.Value == user.Email);
        jwt.Claims.Should().Contain(c => c.Type == "Position" && c.Value == user.Position.ToString());
        jwt.Claims.Should().Contain(c => c.Type == ClaimTypes.Role && c.Value == "Employee");
    }

    [Fact]
    public async Task GenerateAccessTokenAsync_ShouldIncludeAllRoles_WhenUserHasMultipleRoles()
    {
        // Arrange
        var user = CreateValidUser();
        SetupValidRolesForUser(user, "Employee", "Admin");

        // Act
        var result = await _tokenService.GenerateAccessTokenAsync(user);

        // Assert
        var handler = new JwtSecurityTokenHandler();
        var jwt = handler.ReadJwtToken(result.Value);

        jwt.Claims.Where(c => c.Type == ClaimTypes.Role).Select(c => c.Value)
            .Should().BeEquivalentTo(new[] { "Employee", "Admin" });
    }

    #endregion

    #region GenerateRefreshTokenAsync

    [Fact]
    public async Task GenerateRefreshTokenAsync_ShouldAddRefreshToken_WithCorrectUserId()
    {
        // Arrange
        var userId = Guid.NewGuid();

        // Act
        await _tokenService.GenerateRefreshTokenAsync(userId);

        // Assert
        _refreshTokensRepositoryMock.Verify(
            r => r.AddAsync(It.Is<RefreshToken>(t => t.UserId == userId), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task GenerateRefreshTokenAsync_ShouldCallSaveChanges()
    {
        // Arrange
        var userId = Guid.NewGuid();

        // Act
        await _tokenService.GenerateRefreshTokenAsync(userId);

        // Assert
        _refreshTokensRepositoryMock.Verify(
            r => r.SaveChangesAsync(It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task GenerateRefreshTokenAsync_ShouldReturnSuccessWithNonEmptyToken()
    {
        // Arrange
        var userId = Guid.NewGuid();

        // Act
        var result = await _tokenService.GenerateRefreshTokenAsync(userId);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task GenerateRefreshTokenAsync_ShouldReturnDifferentTokens_OnEachCall()
    {
        // Arrange
        var userId = Guid.NewGuid();

        // Act
        var result1 = await _tokenService.GenerateRefreshTokenAsync(userId);
        var result2 = await _tokenService.GenerateRefreshTokenAsync(userId);

        // Assert
        result1.Value.Should().NotBe(result2.Value);
    }

    #endregion

    #region GenerateNewTokensAsync

    [Fact]
    public async Task GenerateNewTokensAsync_ShouldReturnMappedFailure_WhenAccessTokenGenerationFails()
    {
        // Arrange
        var user = CreateValidUser(userName: ""); // forces access token failure

        // Act
        var result = await _tokenService.GenerateNewTokensAsync(user);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Be("access token Cannt be Created because of missing userName or Email");
        result.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        // refresh token should never be generated if access token fails
        _refreshTokensRepositoryMock.Verify(
            r => r.AddAsync(It.IsAny<RefreshToken>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    // NOTE: GenerateRefreshTokenAsync never actually returns a failure result (it always
    // succeeds), so the "refresh token generation fails" branch in GenerateNewTokensAsync
    // is currently unreachable and cannot be tested without refactoring the service.

    [Fact]
    public async Task GenerateNewTokensAsync_ShouldReturnSuccessWithBothTokens_WhenAllStepsSucceed()
    {
        // Arrange
        var user = CreateValidUser();
        SetupValidRolesForUser(user, "Employee");

        // Act
        var result = await _tokenService.GenerateNewTokensAsync(user);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value!.AccessToken.Should().NotBeNullOrWhiteSpace();
        result.Value.RefreshToken.Should().NotBeNullOrWhiteSpace();

        _refreshTokensRepositoryMock.Verify(
            r => r.AddAsync(It.Is<RefreshToken>(t => t.UserId == user.Id), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    #endregion

    #region RegenerateTokensAsync

    [Fact]
    public async Task RegenerateTokensAsync_ShouldReturnMappedFailure_WhenGettingCookieFails()
    {
        // Arrange
        _cookiesServicesMock
            .Setup(c => c.Get(RefreshTokenCookieKey))
            .Returns(Result<string>.Failure("cookie not found", HttpStatusCode.BadRequest));

        // Act
        var result = await _tokenService.RegenerateTokensAsync();

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Be("cookie not found");
        result.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        _refreshTokensRepositoryMock.Verify(
            r => r.RemoveRefreshTokenByRefreshTokenString(It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task RegenerateTokensAsync_ShouldReturnFailure_WhenRefreshTokenNotFound()
    {
        // Arrange
        var cookieToken = "some-refresh-token";
        _cookiesServicesMock
            .Setup(c => c.Get(RefreshTokenCookieKey))
            .Returns(Result<string>.Success(cookieToken));

        _refreshTokensRepositoryMock
            .Setup(r => r.RemoveRefreshTokenByRefreshTokenString(cookieToken, It.IsAny<CancellationToken>()))
            .ReturnsAsync((RefreshToken?)null);

        // Act
        var result = await _tokenService.RegenerateTokensAsync();

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Be("refresh token expired or not found");
        result.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task RegenerateTokensAsync_ShouldReturnFailure_WhenRefreshTokenIsExpired()
    {
        // Arrange
        var cookieToken = "some-refresh-token";
        var expiredToken = CreateRefreshToken(expires: DateTime.UtcNow.AddMinutes(-5));

        _cookiesServicesMock
            .Setup(c => c.Get(RefreshTokenCookieKey))
            .Returns(Result<string>.Success(cookieToken));

        _refreshTokensRepositoryMock
            .Setup(r => r.RemoveRefreshTokenByRefreshTokenString(cookieToken, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expiredToken);

        // Act
        var result = await _tokenService.RegenerateTokensAsync();

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Be("Expired refresh token");
        result.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        _userManagerMock.Verify(
            m => m.FindByIdAsync(It.IsAny<string>()),
            Times.Never);
    }

    [Fact]
    public async Task RegenerateTokensAsync_ShouldReturnFailure_WhenUserNotFound()
    {
        // Arrange
        var cookieToken = "some-refresh-token";
        var validToken = CreateRefreshToken(expires: DateTime.UtcNow.AddMinutes(30));

        _cookiesServicesMock
            .Setup(c => c.Get(RefreshTokenCookieKey))
            .Returns(Result<string>.Success(cookieToken));

        _refreshTokensRepositoryMock
            .Setup(r => r.RemoveRefreshTokenByRefreshTokenString(cookieToken, It.IsAny<CancellationToken>()))
            .ReturnsAsync(validToken);

        _userManagerMock
            .Setup(m => m.FindByIdAsync(validToken.UserId.ToString()))
            .ReturnsAsync((ApplicationUser?)null);

        // Act
        var result = await _tokenService.RegenerateTokensAsync();

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Be("User not found");
        result.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task RegenerateTokensAsync_ShouldReturnSuccessWithNewTokens_WhenAllStepsSucceed()
    {
        // Arrange
        var cookieToken = "some-refresh-token";
        var user = CreateValidUser();
        var validToken = CreateRefreshToken(userId: user.Id, expires: DateTime.UtcNow.AddMinutes(30));

        _cookiesServicesMock
            .Setup(c => c.Get(RefreshTokenCookieKey))
            .Returns(Result<string>.Success(cookieToken));

        _refreshTokensRepositoryMock
            .Setup(r => r.RemoveRefreshTokenByRefreshTokenString(cookieToken, It.IsAny<CancellationToken>()))
            .ReturnsAsync(validToken);

        _userManagerMock
            .Setup(m => m.FindByIdAsync(user.Id.ToString()))
            .ReturnsAsync(user);

        SetupValidRolesForUser(user, "Employee");

        // Act
        var result = await _tokenService.RegenerateTokensAsync();

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value!.AccessToken.Should().NotBeNullOrWhiteSpace();
        result.Value.RefreshToken.Should().NotBeNullOrWhiteSpace();
    }

    #endregion

    #region UpdateUserTokensAsync

    [Fact]
    public async Task UpdateUserTokensAsync_ShouldReturnFailure_WhenRegenerateTokensFails()
    {
        // Arrange
        _cookiesServicesMock
            .Setup(c => c.Get(RefreshTokenCookieKey))
            .Returns(Result<string>.Failure("cookie not found", HttpStatusCode.BadRequest));

        // Act
        var result = await _tokenService.UpdateUserTokensAsync();

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Be("cookie not found");

        _cookiesServicesMock.Verify(
            c => c.AddTokens(It.IsAny<AccessAndRefreshTokenDTO>()),
            Times.Never);
    }

    [Fact]
    public async Task UpdateUserTokensAsync_ShouldReturnMappedFailure_WhenAddingTokensToCookiesFails()
    {
        // Arrange
        var cookieToken = "some-refresh-token";
        var user = CreateValidUser();
        var validToken = CreateRefreshToken(userId: user.Id, expires: DateTime.UtcNow.AddMinutes(30));

        _cookiesServicesMock
            .Setup(c => c.Get(RefreshTokenCookieKey))
            .Returns(Result<string>.Success(cookieToken));

        _refreshTokensRepositoryMock
            .Setup(r => r.RemoveRefreshTokenByRefreshTokenString(cookieToken, It.IsAny<CancellationToken>()))
            .ReturnsAsync(validToken);

        _userManagerMock
            .Setup(m => m.FindByIdAsync(user.Id.ToString()))
            .ReturnsAsync(user);

        SetupValidRolesForUser(user, "Employee");

        _cookiesServicesMock
            .Setup(c => c.AddTokens(It.IsAny<AccessAndRefreshTokenDTO>()))
            .Returns(Result.Failure("failed to set cookies", HttpStatusCode.BadRequest));

        // Act
        var result = await _tokenService.UpdateUserTokensAsync();

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Be("failed to set cookies");
    }

    [Fact]
    public async Task UpdateUserTokensAsync_ShouldReturnSuccessWithNewTokens_WhenAllStepsSucceed()
    {
        // Arrange
        var cookieToken = "some-refresh-token";
        var user = CreateValidUser();
        var validToken = CreateRefreshToken(userId: user.Id, expires: DateTime.UtcNow.AddMinutes(30));

        _cookiesServicesMock
            .Setup(c => c.Get(RefreshTokenCookieKey))
            .Returns(Result<string>.Success(cookieToken));

        _refreshTokensRepositoryMock
            .Setup(r => r.RemoveRefreshTokenByRefreshTokenString(cookieToken, It.IsAny<CancellationToken>()))
            .ReturnsAsync(validToken);

        _userManagerMock
            .Setup(m => m.FindByIdAsync(user.Id.ToString()))
            .ReturnsAsync(user);

        SetupValidRolesForUser(user, "Employee");

        _cookiesServicesMock
            .Setup(c => c.AddTokens(It.IsAny<AccessAndRefreshTokenDTO>()))
            .Returns(Result.Success());

        // Act
        var result = await _tokenService.UpdateUserTokensAsync();

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value!.AccessToken.Should().NotBeNullOrWhiteSpace();

        _cookiesServicesMock.Verify(
            c => c.AddTokens(It.Is<AccessAndRefreshTokenDTO>(t =>
                t.AccessToken == result.Value.AccessToken &&
                t.RefreshToken == result.Value.RefreshToken)),
            Times.Once);
    }

    #endregion
}