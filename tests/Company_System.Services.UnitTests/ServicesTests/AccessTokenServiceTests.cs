using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Security.Claims;
using AutoFixture;
using FluentAssertions;
using HR_System.Core.Domain.Identity;
using HR_System.Core.Enums;
using HR_System.Infrastructure.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Moq;

namespace HR_System.Core.UnitTests.ServicesTests;

public class AccessTokenServiceTests : ServiceTestBase
{
    private readonly AccessTokenService _accessTokenService;
    private readonly Mock<UserManager<ApplicationUser>> _userManagerMock;
    private readonly IConfiguration _configuration;
    private const string JwtKey = "this-is-a-super-secret-testing-key-32bytes+";
    private const string JwtIssuer = "test-issuer";
    private const string JwtAudience = "test-audience";
    public AccessTokenServiceTests(ITestOutputHelper output) : base(output)
    {
        var storeMock = new Mock<IUserStore<ApplicationUser>>();
        _userManagerMock = new Mock<UserManager<ApplicationUser>>(storeMock.Object, null !, null !, null !, null !, null !, null !, null !, null !);
        var inMemorySettings = new Dictionary<string, string?>
        {
            {
                "Jwt:Key",
                JwtKey
            },
            {
                "Jwt:Issuer",
                JwtIssuer
            },
            {
                "Jwt:Audience",
                JwtAudience
            },
            {
                "Jwt:AccessTokenLifeTime",
                "15"
            }
        };
        _configuration = new ConfigurationBuilder().AddInMemoryCollection(inMemorySettings).Build();
        _accessTokenService = new AccessTokenService(_userManagerMock.Object, Microsoft.Extensions.Options.Options.Create(new HR_System.Core.Options.JWTOptions { Key = JwtKey, Issuer = JwtIssuer, Audience = JwtAudience, AccessTokenLifeTime = 15 }));
    }

    private ApplicationUser CreateValidUser(string? userName = null, string? email = null)
    {
        return Fixture.Build<ApplicationUser>()
            .With(u => u.UserName, userName ?? "john.doe")
            .With(
                u => u.Email,
                email ?? "john.doe@example.com"
            )
            .With(
            u => u.Position,
            PositionsEnum.Employee
        )
            .Without(u => u.RefreshTokens)
            .Create();
    }

    // ---------- GenerateAccessTokenAsync ----------
    #region GenerateAccessTokenAsync
    [Fact]
    public async Task GenerateAccessTokenAsync_ShouldReturnFailure_WhenUserNameIsMissing()
    {
        // Arrange
        LogArrange("Scenario", nameof(GenerateAccessTokenAsync_ShouldReturnFailure_WhenUserNameIsMissing));

        var user = CreateValidUser(userName: "");

        // Act
        var result = await _accessTokenService.GenerateAccessTokenAsync(user);
        LogAct(result);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task GenerateAccessTokenAsync_ShouldReturnFailure_WhenEmailIsMissing()
    {
        // Arrange
        LogArrange("Scenario", nameof(GenerateAccessTokenAsync_ShouldReturnFailure_WhenEmailIsMissing));

        var user = CreateValidUser(email: "");

        // Act
        var result = await _accessTokenService.GenerateAccessTokenAsync(user);
        LogAct(result);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task GenerateAccessTokenAsync_ShouldReturnFailure_WhenUserHasNoRoles()
    {
        // Arrange
        LogArrange("Scenario", nameof(GenerateAccessTokenAsync_ShouldReturnFailure_WhenUserHasNoRoles));

        var user = CreateValidUser();
        _userManagerMock.Setup(m => m.GetRolesAsync(user))
            .ReturnsAsync(new List<string>());

        // Act
        var result = await _accessTokenService.GenerateAccessTokenAsync(user);
        LogAct(result);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Be("User has no roles.");
    }

    [Fact]
    public async Task GenerateAccessTokenAsync_ShouldReturnSuccessWithValidJwt_WhenAllDataValid()
    {
        // Arrange
        LogArrange("Scenario", nameof(GenerateAccessTokenAsync_ShouldReturnSuccessWithValidJwt_WhenAllDataValid));

        var user = CreateValidUser();
        _userManagerMock.Setup(m => m.GetRolesAsync(user))
            .ReturnsAsync(new List<string> { "Employee" });

        // Act
        var result = await _accessTokenService.GenerateAccessTokenAsync(user);
        LogAct(result);

        // Assert
        result.IsSuccess.Should().BeTrue();
        var handler = new JwtSecurityTokenHandler();
        var jwt = handler.ReadJwtToken(result.Value!.AccessToken);
        jwt.Issuer.Should().Be(JwtIssuer);
        jwt.Audiences.Should().Contain(JwtAudience);
        jwt.Claims.Should().Contain(c => c.Type == ClaimTypes.Role && c.Value == "Employee");
    }
    #endregion
}
