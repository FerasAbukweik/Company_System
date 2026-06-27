using AutoFixture;
using FluentAssertions;
using HR_System.Core.common;
using HR_System.Core.Domain.Entities;
using HR_System.Core.Domain.Idnetity;
using HR_System.Core.Interfaces.RepositoryContracts;
using HR_System.Core.Interfaces.ServiceContracts.ICookieServices;
using HR_System.Core.Interfaces.ServiceContracts.ITokenServices;
using HR_System.Infrastructure.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Moq;
using Xunit.Abstractions;

namespace HR_System.Core.UnitTests.ServicesTests;

public class TokenServiceTests
{
    private readonly ITokenService _tokenService;
    private readonly Mock<IRefreshTokenRepository> _refreshTokenRepositoryMock;
    private readonly Mock<UserManager<ApplicationUser>> _userManagerMock;
    private readonly IFixture _fixture;
    private readonly ITestOutputHelper _output;
    private readonly Mock<ICookieService> _cookieService;

    public TokenServiceTests(ITestOutputHelper output)
    {
        _output = output;
        
        // fixture
        _fixture = new Fixture();
        _fixture.Behaviors.OfType<ThrowingRecursionBehavior>().ToList()
            .ForEach(b => _fixture.Behaviors.Remove(b));
        _fixture.Behaviors.Add(new OmitOnRecursionBehavior());
        
        // refresh token repository mock
        _refreshTokenRepositoryMock = new Mock<IRefreshTokenRepository>();
        _cookieService = new Mock<ICookieService>();
        
        // user manager mock
        var storeMock = new Mock<IUserStore<ApplicationUser>>();
        _userManagerMock = new Mock<UserManager<ApplicationUser>>(storeMock.Object, null!, null!, null!, null!, null!, null!, null!, null!);
        
        // configuration
        var inMemoryCollection = new Dictionary<string, string> {
            {"Jwt:Issuer", "dummy data"},
            {"Jwt:Audience", "dummy data"},
            {"Jwt:Key", "dummyKey_____DMOPIAJP#YU(ejD#*u80132129e9uQ@!J*4324EUjdokasJDIPJqpwu*&U!@e309123e098u(DJ"},
            {"AccessTokenLifeTime", "15"},
            {"RefreshTokenLifeTime", "10080"}
        };
        
        // getCurrentUserService Mock
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(inMemoryCollection!)
            .Build();

            
        _tokenService = new TokenService(_cookieService.Object,_refreshTokenRepositoryMock.Object, _userManagerMock.Object, configuration);
    }

    #region GenerateAccessTokenTests
    [Fact]
    public async Task GenerateAccessTokenTest_ValidInput_ShoudSuccess()
    {
        // Arrange
        var testUser = _fixture.Create<ApplicationUser>();
        _output.WriteLine($"User:\n{testUser.ToString()}");
        
        _userManagerMock.Setup(t => t.GetRolesAsync(It.IsAny<ApplicationUser>())).ReturnsAsync(new List<string>(){"test"});

        // Act
        var actualResult = await _tokenService.GenerateAccessTokenAsync(testUser);
        _output.WriteLine($"Actual Token:\n{actualResult?.Value ?? "null"}");

        // Assert
        actualResult.Should().NotBeNull();
        actualResult.IsSuccess.Should().BeTrue();
        actualResult.Value.Should().NotBeNull();
        actualResult.Value.Should().BeAssignableTo<string>();
    }

    [Fact]
    public async Task GenerateAccessTokenTest_NoRole_ShoudFail()
    {
        // Arrange
        var testUser = _fixture.Create<ApplicationUser>();
        _output.WriteLine($"User:\n{testUser.ToString()}");
        
        // no roles
        _userManagerMock.Setup(t => t.GetRolesAsync(It.IsAny<ApplicationUser>())).ReturnsAsync(new List<string>(){});

        // Act
        var actualResult = await _tokenService.GenerateAccessTokenAsync(testUser);
        _output.WriteLine($"Actual Token: {actualResult?.Value ?? "null"}");

        // Assert
        actualResult.Should().NotBeNull();
        actualResult.IsSuccess.Should().BeFalse();
        actualResult.Value.Should().BeNull();
    }

    [Fact]
    public async Task GenerateAccessTokenTest_EmptyUserNameAndEmail_ShoudFail()
    {
        // Arrange
        var testUser = _fixture.Build<ApplicationUser>()
            .With(t => t.UserName, string.Empty)
            .With(t => t.Email, string.Empty)
            .Create();
        _output.WriteLine($"User:\n{testUser.ToString()}");
        
        _userManagerMock.Setup(t => t.GetRolesAsync(It.IsAny<ApplicationUser>())).ReturnsAsync(new List<string>(){"test role"});

        // Act
        var actualResult = await _tokenService.GenerateAccessTokenAsync(testUser);
        _output.WriteLine($"Actual Token: {actualResult?.Value ?? "null"}");

        // Assert
        actualResult.Should().NotBeNull();
        actualResult.IsSuccess.Should().BeFalse();
        actualResult.Value.Should().BeNull();
    }
    
    #endregion

    #region GenerateRefreshTokenTests
    [Fact]
    public async Task GenerateRefreshTokenTest_ValidInput_ShouldSuccess()
    {
        // Arrange
        Guid userId = Guid.NewGuid();
        _refreshTokenRepositoryMock.Setup(t => t.AddAsync(It.IsAny<RefreshToken>()));
        _output.WriteLine($"UserId: {userId}");

        // Act
        var acutal = await _tokenService.GenerateRefreshTokenAsync(userId);
        _output.WriteLine($"acutal Token: {acutal?.Value?? "null"}");

        // Assert
        acutal.Should().NotBeNull();
        acutal.IsSuccess.Should().BeTrue();
        acutal.Value.Should().NotBeNull();
        acutal.Value.Should().BeAssignableTo<string>();
    }
    #endregion

    #region IsRefreshTokenValidTests

    [Fact]
    public async Task IsRefreshTokenValid_ValidData_ShouldSucceed()
    {
        // Arrange
        var userId = Guid.NewGuid();
        
        _cookieService.Setup(t => t.Get(It.IsAny<string>()))
            .Returns(Result<string>.Success(_fixture.Create<string>()));

        _refreshTokenRepositoryMock.Setup(t => t.FindRefreshTokenByRefreshTokenStringAsync(It.IsAny<string>()))
            .ReturnsAsync(
                _fixture.Build<RefreshToken>()
                    .With(rt => rt.UserId, userId)
                    .With(rt => rt.Expires, DateTime.UtcNow.AddDays(1))
                    .Create()
                );
        
        _output.WriteLine($"UserId: {userId}");

        // Act
        var actual = await _tokenService.IsRefreshTokenValid(userId);
        _output.WriteLine($"Actual: {actual}");

        // Assert
        
        actual.Should().NotBeNull();
        actual.IsSuccess.Should().BeTrue();
        actual.Value.Should().NotBeNull();
    }
    
    [Fact]
    public async Task IsRefreshTokenValid_ExpiredToken_ShouldSucceed()
    {
        // Arrange
        var userId = Guid.NewGuid();
        
        _cookieService.Setup(t => t.Get(It.IsAny<string>()))
            .Returns(Result<string>.Success(_fixture.Create<string>()));

        _refreshTokenRepositoryMock.Setup(t => t.FindRefreshTokenByRefreshTokenStringAsync(It.IsAny<string>()))
            .ReturnsAsync(
                _fixture.Build<RefreshToken>()
                    .With(rt => rt.UserId, userId)
                    .With(rt => rt.Expires, DateTime.UtcNow.AddDays(-1))
                    .Create()
            );
        
        _output.WriteLine($"UserId: {userId}");

        // Act
        var actual = await _tokenService.IsRefreshTokenValid(userId);
        _output.WriteLine($"Actual: {actual}");

        // Assert
        
        actual.Should().NotBeNull();
        actual.IsSuccess.Should().BeFalse();
        actual.Value.Should().BeNull();
    }
    
    
    [Fact]
    public async Task IsRefreshTokenValid_NoTokenInCookies_ShouldSucceed()
    {
        // Arrange
        var userId = Guid.NewGuid();
        
        _cookieService.Setup(t => t.Get(It.IsAny<string>()))
            .Returns(Result<string>.Failure(""));

        _refreshTokenRepositoryMock.Setup(t => t.FindRefreshTokenByRefreshTokenStringAsync(It.IsAny<string>()))
            .ReturnsAsync(
                _fixture.Build<RefreshToken>()
                    .With(rt => rt.UserId, userId)
                    .With(rt => rt.Expires, DateTime.UtcNow.AddDays(-1))
                    .Create()
            );
        
        _output.WriteLine($"UserId: {userId}");

        // Act
        var actual = await _tokenService.IsRefreshTokenValid(userId);
        _output.WriteLine($"Actual: {actual}");

        // Assert
        
        actual.Should().NotBeNull();
        actual.IsSuccess.Should().BeFalse();
        actual.Value.Should().BeNull();
    }
    
    
    [Fact]
    public async Task IsRefreshTokenValid_NotUserRefreshToken_ShouldSucceed()
    {
        // Arrange
        var userId = Guid.NewGuid();
        
        _cookieService.Setup(t => t.Get(It.IsAny<string>()))
            .Returns(Result<string>.Success(_fixture.Create<string>()));

        _refreshTokenRepositoryMock.Setup(t => t.FindRefreshTokenByRefreshTokenStringAsync(It.IsAny<string>()))
            .ReturnsAsync(
                _fixture.Build<RefreshToken>()
                    .With(rt => rt.Expires, DateTime.UtcNow.AddDays(1))
                    .Create()
            );
        
        _output.WriteLine($"UserId: {userId}");

        // Act
        var actual = await _tokenService.IsRefreshTokenValid(userId);
        _output.WriteLine($"Actual: {actual}");

        // Assert
        
        actual.Should().NotBeNull();
        actual.IsSuccess.Should().BeFalse();
        actual.Value.Should().BeNull();
    }

    #endregion
    
}