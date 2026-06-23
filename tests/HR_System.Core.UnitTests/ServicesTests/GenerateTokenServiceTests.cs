using AutoFixture;
using FluentAssertions;
using HR_System.Core.Domain.Entities;
using HR_System.Core.Domain.Idnetity;
using HR_System.Core.Domain.RepositoryContracts;
using HR_System.Core.Helpers;
using HR_System.Core.ServiceContracts.ICurrentUserServices;
using HR_System.Core.ServiceContracts.ITokenServices;
using HR_System.Core.Services.TokenServices;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Moq;
using Xunit.Abstractions;

namespace HR_System.Core.UnitTests.ServicesTests;

public class GenerateTokenServiceTests
{
    private readonly IGenerateTokenService _generateTokenService;
    private readonly Mock<IRefreshTokenRepository> _refreshTokenRepositoryMock;
    private readonly Mock<UserManager<ApplicationUser>> _userManagerMock;
    private readonly Mock<IGetCurrentUserService>  _getCurrentUserServiceMock;
    private readonly IFixture _fixture;
    private readonly ITestOutputHelper _output;

    public GenerateTokenServiceTests(ITestOutputHelper output)
    {
        _output = output;
        
        // fixture
        _fixture = new Fixture();
        _fixture.Behaviors.OfType<ThrowingRecursionBehavior>().ToList()
            .ForEach(b => _fixture.Behaviors.Remove(b));
        _fixture.Behaviors.Add(new OmitOnRecursionBehavior());
        
        // refreshtoken repository mock
        _refreshTokenRepositoryMock = new Mock<IRefreshTokenRepository>();
        
        // usermanager mock
        var storeMock = new Mock<IUserStore<ApplicationUser>>();
        _userManagerMock = new Mock<UserManager<ApplicationUser>>(storeMock.Object, null, null, null, null, null, null, null, null);
        
        // configuration
        var inMemoryCollection = new Dictionary<string, string> {
            {"Jwt:Issuer", "dummy data"},
            {"Jwt:Audience", "dummy data"},
            {"Jwt:Key", "dummyDataDMOPIAJP#YU(ejD#*u80132129e9uQ@!J*4324EUjdokasJDIPJqpwu*&U!@e309123e098u(DJ"},
            {"AccessTokenLifeTime", "15"},
            {"RefreshTokenLifeTime", "10080"}
        };
        
        // getCurrentUserService Mock
        _getCurrentUserServiceMock =  new Mock<IGetCurrentUserService>();

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(inMemoryCollection)
            .Build();

            
        _generateTokenService = new GenerateTokenService(_userManagerMock.Object, configuration, _refreshTokenRepositoryMock.Object, _getCurrentUserServiceMock.Object);
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
        var actualResult = await _generateTokenService.GenerateAccessTokenAsync(testUser);
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
        var actualResult = await _generateTokenService.GenerateAccessTokenAsync(testUser);
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
        var actualResult = await _generateTokenService.GenerateAccessTokenAsync(testUser);
        _output.WriteLine($"Actual Token: {actualResult?.Value ?? "null"}");

        // Assert
        actualResult.Should().NotBeNull();
        actualResult.IsSuccess.Should().BeFalse();
        actualResult.Value.Should().BeNull();
    }
    
    #endregion

    #region GenerateRefreshTokenTests
    [Fact]
    public async Task GenerateRefreshTokenTest_ValidInput_ShoudSuccess()
    {
        // Arrange
        Guid testUserId = Guid.NewGuid();
        _refreshTokenRepositoryMock.Setup(t => t.AddAsync(It.IsAny<RefreshToken>()))
            .ReturnsAsync(Result<RefreshToken>.Success(_fixture.Create<RefreshToken>()));
        
        _output.WriteLine($"UserId: {testUserId}");

        // Act
        var acutal = await _generateTokenService.GenerateRefreshTokenAsync(testUserId);
        _output.WriteLine($"acutal Token: {acutal?.Value?? "null"}");

        // Assert
        acutal.Should().NotBeNull();
        acutal.IsSuccess.Should().BeTrue();
        acutal.Value.Should().NotBeNull();
        acutal.Value.Should().BeAssignableTo<string>();
    }
    
    [Fact]
    public async Task GenerateRefreshTokenTest_FailedToAddToDB_shouldFail()
    {
        // Arrange
        Guid testUserId = Guid.NewGuid();
        _refreshTokenRepositoryMock.Setup(t => t.AddAsync(It.IsAny<RefreshToken>()))
            .ReturnsAsync(Result<RefreshToken>.Failure("test"));
        
        _output.WriteLine($"UserId: {testUserId}");

        // Act
        var acutal = await _generateTokenService.GenerateRefreshTokenAsync(testUserId);
        _output.WriteLine($"acutal Token: {acutal?.Value?? "null"}");

        // Assert
        acutal.Should().NotBeNull();
        acutal.IsSuccess.Should().BeFalse();
        acutal.Value.Should().BeNull();
    }
    #endregion
    
}