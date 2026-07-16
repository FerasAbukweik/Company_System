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
using Microsoft.Extensions.Logging;
using Moq;

namespace TestProject1.ServicesTests;

public class AccessTokenServiceTests
{
    private readonly AccessTokenService _accessTokenService;
    private readonly Mock<UserManager<ApplicationUser>> _userManagerMock;
    private readonly IConfiguration _configuration;
    private readonly IFixture _fixture;

    private const string JwtKey = "this-is-a-super-secret-testing-key-32bytes+";
    private const string JwtIssuer = "test-issuer";
    private const string JwtAudience = "test-audience";

    public AccessTokenServiceTests()
    {
        _fixture = new Fixture();
        _fixture.Behaviors.OfType<ThrowingRecursionBehavior>().ToList().ForEach(b => _fixture.Behaviors.Remove(b));
        _fixture.Behaviors.Add(new OmitOnRecursionBehavior());

        var storeMock = new Mock<IUserStore<ApplicationUser>>();
        _userManagerMock = new Mock<UserManager<ApplicationUser>>(
            storeMock.Object, null!, null!, null!, null!, null!, null!, null!, null!);

        var inMemorySettings = new Dictionary<string, string?>
        {
            { "Jwt:Key", JwtKey },
            { "Jwt:Issuer", JwtIssuer },
            { "Jwt:Audience", JwtAudience },
            { "Jwt:AccessTokenLifeTime", "15" }
        };
        _configuration = new ConfigurationBuilder().AddInMemoryCollection(inMemorySettings).Build();

        var loggerMock = new Mock<ILogger<AccessTokenService>>();

        _accessTokenService = new AccessTokenService(_userManagerMock.Object, _configuration, loggerMock.Object);
    }

    private ApplicationUser CreateValidUser(string? userName = null, string? email = null)
    {
        return _fixture.Build<ApplicationUser>()
            .With(u => u.UserName, userName ?? "john.doe")
            .With(u => u.Email, email ?? "john.doe@example.com")
            .With(u => u.Position, PositionsEnum.Employee)
            .Without(u => u.RefreshTokens)
            .Create();
    }

    [Fact]
    public async Task GenerateAccessTokenAsync_ShouldReturnFailure_WhenUserNameIsMissing()
    {
        var user = CreateValidUser(userName: "");
        var result = await _accessTokenService.GenerateAccessTokenAsync(user);

        result.IsSuccess.Should().BeFalse();
        result.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task GenerateAccessTokenAsync_ShouldReturnFailure_WhenEmailIsMissing()
    {
        var user = CreateValidUser(email: "");
        var result = await _accessTokenService.GenerateAccessTokenAsync(user);

        result.IsSuccess.Should().BeFalse();
        result.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task GenerateAccessTokenAsync_ShouldReturnFailure_WhenUserHasNoRoles()
    {
        var user = CreateValidUser();
        _userManagerMock.Setup(m => m.GetRolesAsync(user)).ReturnsAsync(new List<string>());

        var result = await _accessTokenService.GenerateAccessTokenAsync(user);

        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Be("User has no roles.");
    }

    [Fact]
    public async Task GenerateAccessTokenAsync_ShouldReturnSuccessWithValidJwt_WhenAllDataValid()
    {
        var user = CreateValidUser();
        _userManagerMock.Setup(m => m.GetRolesAsync(user)).ReturnsAsync(new List<string> { "Employee" });

        var result = await _accessTokenService.GenerateAccessTokenAsync(user);

        result.IsSuccess.Should().BeTrue();
        var handler = new JwtSecurityTokenHandler();
        var jwt = handler.ReadJwtToken(result.Value);

        jwt.Issuer.Should().Be(JwtIssuer);
        jwt.Audiences.Should().Contain(JwtAudience);
        jwt.Claims.Should().Contain(c => c.Type == ClaimTypes.Role && c.Value == "Employee");
    }
}