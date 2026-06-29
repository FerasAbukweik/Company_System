using AutoFixture;
using FluentAssertions;
using HR_System.Core.common;
using HR_System.Core.Constraints;
using HR_System.Core.Domain.Entities;
using HR_System.Core.Domain.Identity;
using HR_System.Core.Interfaces.RepositoryContracts;
using HR_System.Core.Interfaces.ServiceContracts.ICookieServices;
using HR_System.Core.Interfaces.ServiceContracts.ITokenServices;
using HR_System.Infrastructure.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using Moq;
using Xunit.Abstractions;
using Task = System.Threading.Tasks.Task;

namespace HR_System.Core.UnitTests.ServicesTests;

public class TokenServiceTests
{
    private readonly ITokenService _tokenService;
    private readonly Mock<IRefreshTokensRepository> _refreshTokenRepositoryMock;
    private readonly Mock<UserManager<ApplicationUser>> _userManagerMock;
    private readonly IFixture _fixture;
    private readonly ITestOutputHelper _output;
    private readonly Mock<ICookiesServices> _cookieService;
    private readonly Mock<IOptions<CookieKeys>> _cookieKeysMock;

    public TokenServiceTests(ITestOutputHelper output)
    {
        _output = output;

        _fixture = new Fixture();
        _fixture.Behaviors.OfType<ThrowingRecursionBehavior>().ToList()
            .ForEach(b => _fixture.Behaviors.Remove(b));
        _fixture.Behaviors.Add(new OmitOnRecursionBehavior());

        _refreshTokenRepositoryMock = new Mock<IRefreshTokensRepository>();
        _cookieService = new Mock<ICookiesServices>();
        _cookieKeysMock = new Mock<IOptions<CookieKeys>>();

        var storeMock = new Mock<IUserStore<ApplicationUser>>();
        _userManagerMock = new Mock<UserManager<ApplicationUser>>(storeMock.Object, null!, null!, null!, null!, null!, null!, null!, null!);

        var inMemoryCollection = new Dictionary<string, string> {
            {"Jwt:Issuer", "dummy data"},
            {"Jwt:Audience", "dummy data"},
            {"Jwt:Key", "dummyKey_____DMOPIAJP#YU(ejD#*u80132129e9uQ@!J*4324EUjdokasJDIPJqpwu*&U!@e309123e098u(DJ"},
            {"AccessTokenLifeTime", "15"},
            {"RefreshTokenLifeTime", "10080"}
        };

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(inMemoryCollection!)
            .Build();

        _tokenService = new TokenService(_cookieService.Object, _refreshTokenRepositoryMock.Object, _userManagerMock.Object, configuration, _cookieKeysMock.Object);
    }

    #region GenerateAccessTokenTests

    [Fact]
    public async Task GenerateAccessTokenTest_ValidInput_ShoudSuccess()
    {
        // Arrange
        var testUser = _fixture.Create<ApplicationUser>();

        _userManagerMock
            .Setup(t => t.GetRolesAsync(It.IsAny<ApplicationUser>()))
            .ReturnsAsync(["test"]);

        _output.WriteLine($"User:\n{testUser.ToString()}");

        // Act
        var actual = await _tokenService.GenerateAccessTokenAsync(testUser);
        _output.WriteLine($"IsSuccess : {actual.IsSuccess}");
        _output.WriteLine($"Value     : {actual.Value ?? "null"}");

        // Assert
        actual.Should().NotBeNull();
        actual.IsSuccess.Should().BeTrue();
        actual.Value.Should().NotBeNull();
        actual.Value.Should().BeAssignableTo<string>();
    }

    [Fact]
    public async Task GenerateAccessTokenTest_NoRole_ShoudFail()
    {
        // Arrange
        var testUser = _fixture.Create<ApplicationUser>();

        _userManagerMock
            .Setup(t => t.GetRolesAsync(It.IsAny<ApplicationUser>()))
            .ReturnsAsync([]);

        _output.WriteLine($"User:\n{testUser.ToString()}");

        // Act
        var actual = await _tokenService.GenerateAccessTokenAsync(testUser);
        _output.WriteLine($"IsSuccess : {actual.IsSuccess}");
        _output.WriteLine($"Value     : {actual.Value ?? "null"}");

        // Assert
        actual.Should().NotBeNull();
        actual.IsSuccess.Should().BeFalse();
        actual.Value.Should().BeNull();
    }

    [Fact]
    public async Task GenerateAccessTokenTest_EmptyUserNameAndEmail_ShoudFail()
    {
        // Arrange
        var testUser = _fixture.Build<ApplicationUser>()
            .With(t => t.UserName, string.Empty)
            .With(t => t.Email, string.Empty)
            .Create();

        _userManagerMock
            .Setup(t => t.GetRolesAsync(It.IsAny<ApplicationUser>()))
            .ReturnsAsync(["test role"]);

        _output.WriteLine($"User:\n{testUser.ToString()}");

        // Act
        var actual = await _tokenService.GenerateAccessTokenAsync(testUser);
        _output.WriteLine($"IsSuccess : {actual.IsSuccess}");
        _output.WriteLine($"Value     : {actual.Value ?? "null"}");

        // Assert
        actual.Should().NotBeNull();
        actual.IsSuccess.Should().BeFalse();
        actual.Value.Should().BeNull();
    }

    #endregion

    #region GenerateRefreshTokenTests

    [Fact]
    public async Task GenerateRefreshTokenTest_ValidInput_ShouldSuccess()
    {
        // Arrange
        var userId = Guid.NewGuid();

        _refreshTokenRepositoryMock
            .Setup(t => t.AddAsync(It.IsAny<RefreshToken>()));

        _output.WriteLine($"UserId: {userId}");

        // Act
        var actual = await _tokenService.GenerateRefreshTokenAsync(userId);
        _output.WriteLine($"IsSuccess : {actual.IsSuccess}");
        _output.WriteLine($"Value     : {actual.Value ?? "null"}");

        // Assert
        actual.Should().NotBeNull();
        actual.IsSuccess.Should().BeTrue();
        actual.Value.Should().NotBeNull();
        actual.Value.Should().BeAssignableTo<string>();
    }

    #endregion

    #region IsRefreshTokenValidTests

    [Fact]
    public async Task IsRefreshTokenValid_ValidData_ShouldSucceed()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var refreshToken = _fixture.Build<RefreshToken>()
            .With(rt => rt.UserId, userId)
            .With(rt => rt.Expires, DateTime.UtcNow.AddDays(1))
            .Create();

        _cookieService
            .Setup(t => t.Get(It.IsAny<string>()))
            .Returns(Result<string>.Success(_fixture.Create<string>()));
        _refreshTokenRepositoryMock
            .Setup(t => t.FindRefreshTokenByRefreshTokenStringAsync(It.IsAny<string>()))
            .ReturnsAsync(refreshToken);
        _cookieKeysMock.Setup(t => t.Value).Returns(new CookieKeys()
        {
            AccessToken = "AccessToken",
            RefreshToken = "RefreshToken"
        });


        _output.WriteLine($"UserId       : {userId}");
        _output.WriteLine($"refreshToken : {refreshToken.ToString()}");

        // Act
        var actual = await _tokenService.IsRefreshTokenValid(userId);
        _output.WriteLine($"IsSuccess : {actual.IsSuccess}");
        _output.WriteLine($"Value     : {actual.Value?.ToString() ?? "null"}");

        // Assert
        actual.Should().NotBeNull();
        actual.IsSuccess.Should().BeTrue();
        actual.Value.Should().NotBeNull();
    }

    [Fact]
    public async Task IsRefreshTokenValid_ExpiredToken_ShouldFail()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var refreshToken = _fixture.Build<RefreshToken>()
            .With(rt => rt.UserId, userId)
            .With(rt => rt.Expires, DateTime.UtcNow.AddDays(-1))
            .Create();

        _cookieService
            .Setup(t => t.Get(It.IsAny<string>()))
            .Returns(Result<string>.Success(_fixture.Create<string>()));
        _refreshTokenRepositoryMock
            .Setup(t => t.FindRefreshTokenByRefreshTokenStringAsync(It.IsAny<string>()))
            .ReturnsAsync(refreshToken);
        _cookieKeysMock.Setup(t => t.Value).Returns(new CookieKeys()
        {
            AccessToken = "AccessToken",
            RefreshToken = "RefreshToken"
        });

        _output.WriteLine($"UserId       : {userId}");
        _output.WriteLine($"refreshToken : {refreshToken.ToString()}");

        // Act
        var actual = await _tokenService.IsRefreshTokenValid(userId);
        _output.WriteLine($"IsSuccess : {actual.IsSuccess}");
        _output.WriteLine($"Value     : {actual.Value?.ToString() ?? "null"}");

        // Assert
        actual.Should().NotBeNull();
        actual.IsSuccess.Should().BeFalse();
        actual.Value.Should().BeNull();
    }

    [Fact]
    public async Task IsRefreshTokenValid_NoTokenInCookies_ShouldFail()
    {
        // Arrange
        var userId = Guid.NewGuid();

        _cookieService
            .Setup(t => t.Get(It.IsAny<string>()))
            .Returns(Result<string>.Failure(""));
        _cookieKeysMock.Setup(t => t.Value).Returns(new CookieKeys()
        {
            AccessToken = "AccessToken",
            RefreshToken = "RefreshToken"
        });

        _output.WriteLine($"UserId: {userId}");
        _output.WriteLine("Cookie returns failure — expecting failure");

        // Act
        var actual = await _tokenService.IsRefreshTokenValid(userId);
        _output.WriteLine($"IsSuccess : {actual.IsSuccess}");
        _output.WriteLine($"Value     : {actual.Value?.ToString() ?? "null"}");

        // Assert
        actual.Should().NotBeNull();
        actual.IsSuccess.Should().BeFalse();
        actual.Value.Should().BeNull();
    }

    [Fact]
    public async Task IsRefreshTokenValid_NotUserRefreshToken_ShouldFail()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var refreshToken = _fixture.Build<RefreshToken>()
            .With(rt => rt.Expires, DateTime.UtcNow.AddDays(1))
            .Create(); // UserId differs from userId

        _cookieService
            .Setup(t => t.Get(It.IsAny<string>()))
            .Returns(Result<string>.Success(_fixture.Create<string>()));
        _refreshTokenRepositoryMock
            .Setup(t => t.FindRefreshTokenByRefreshTokenStringAsync(It.IsAny<string>()))
            .ReturnsAsync(refreshToken);
        _cookieKeysMock.Setup(t => t.Value).Returns(new CookieKeys()
        {
            AccessToken = "AccessToken",
            RefreshToken = "RefreshToken"
        });

        _output.WriteLine($"UserId       : {userId}");
        _output.WriteLine($"refreshToken : {refreshToken.ToString()}");
        _output.WriteLine("Token belongs to different user — expecting failure");

        // Act
        var actual = await _tokenService.IsRefreshTokenValid(userId);
        _output.WriteLine($"IsSuccess : {actual.IsSuccess}");
        _output.WriteLine($"Value     : {actual.Value?.ToString() ?? "null"}");

        // Assert
        actual.Should().NotBeNull();
        actual.IsSuccess.Should().BeFalse();
        actual.Value.Should().BeNull();
    }

    #endregion
}