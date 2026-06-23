using System.ComponentModel.Design;
using System.Security.Claims;
using AutoFixture;
using FluentAssertions;
using HR_System.Core.Domain.Idnetity;
using HR_System.Core.ServiceContracts.ICurrentUserServices;
using HR_System.Core.Services.CurrentUserServices;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Moq;
using Xunit.Abstractions;

namespace HR_System.Core.UnitTests.ServicesTests;

public class GetCurrentUserServiceTests
{
    private readonly IGetCurrentUserService _getCurrentUserService;
    private readonly IFixture _fixture;
    private readonly ITestOutputHelper _output;
    private readonly Mock<IHttpContextAccessor> _contextAccessorMock;
    private readonly Mock<UserManager<ApplicationUser>>  _userManagerMock;

    public GetCurrentUserServiceTests(ITestOutputHelper output)
    {
        _output = output;

        _fixture = new Fixture();
        _fixture.Behaviors.OfType<ThrowingRecursionBehavior>().ToList()
            .ForEach(b => _fixture.Behaviors.Remove(b));
        _fixture.Behaviors.Add(new OmitOnRecursionBehavior());
        
        var userStoreMock = new Mock<IUserStore<ApplicationUser>>();
        _userManagerMock = new Mock<UserManager<ApplicationUser>>(userStoreMock.Object, null, null, null, null, null, null, null, null);
        
        _contextAccessorMock = new Mock<IHttpContextAccessor>();
        
        _getCurrentUserService = new GetCurrentUserService(_contextAccessorMock.Object,  _userManagerMock.Object);
    }

    #region GetUserIdTest
    [Fact]
    public void GetUserIdTest_ValidData_ShouldSuccess()
    {
        // Arrange
        Guid expected = Guid.NewGuid();
        var claims = new List<Claim> 
        { 
            new Claim(ClaimTypes.NameIdentifier, expected.ToString()) 
        };
        var identity = new ClaimsIdentity(claims, "TestAuthType");
        var claimsPrincipal = new ClaimsPrincipal(identity);

        _contextAccessorMock.Setup(t => t.HttpContext.User).Returns(claimsPrincipal);

        _output.WriteLine($"Expected UserId: {expected}");

        // Act
        var actual = _getCurrentUserService.GetUserId();
        _output.WriteLine($"Actual UserId is {actual.Value.ToString()}");

        // Assert
        actual.Should().NotBeNull();
        actual.IsSuccess.Should().BeTrue();
        actual.Value.Should().Be(expected);
    }
    
    [Fact]
    public void GetUserIdTest_BadUserId_ShouldFail()
    {
        // Arrange
        var claims = new List<Claim> 
        { 
            new Claim(ClaimTypes.NameIdentifier, "unassignable to Guid") 
        };
        var identity = new ClaimsIdentity(claims, "TestAuthType");
        var claimsPrincipal = new ClaimsPrincipal(identity);

        _contextAccessorMock.Setup(t => t.HttpContext.User).Returns(claimsPrincipal);

        _output.WriteLine($"Expected UserId: empty string");

        // Act
        var actual = _getCurrentUserService.GetUserId();
        _output.WriteLine($"Actual UserId: {actual.Value.ToString()}");

        // Assert
        actual.Should().NotBeNull();
        actual.IsSuccess.Should().BeFalse();
        actual.Value.Should().Be(Guid.Empty);
    }
    
    #endregion

    #region GetCurrUserObjectTests

    [Fact]
    public async Task GetCurrUserObjectTest_ValidData_ShouldSuccess()
    {
        // Arrange
        ApplicationUser expected = _fixture.Create<ApplicationUser>();
        
        _userManagerMock.Setup(t => t.FindByIdAsync(It.IsAny<string>()))
            .ReturnsAsync(expected);

        var claims = new Claim[]
        {
            new Claim(ClaimTypes.NameIdentifier, expected.Id.ToString())
        };
        var identity = new ClaimsIdentity(claims, "TestAuthType");
        var claimsPrincipal = new ClaimsPrincipal(identity);
        
        _contextAccessorMock.Setup(t => t.HttpContext.User)
            .Returns(claimsPrincipal);
        
        _output.WriteLine($"Expected:\n{expected.ToString()}");
        
        // Act
        var actual = await _getCurrentUserService.GetCurrUserObjectAsync();
        _output.WriteLine($"Actual UserObject:\n{actual?.Value?.ToString() ?? "null"}");
        
        // Assert
        actual.Should().NotBeNull();
        actual.IsSuccess.Should().BeTrue();
        actual.Value.Should().NotBeNull();
        actual.Value.Should().Be(expected);
    }
    
    [Fact]
    public async Task GetCurrUserObjectTest_NoUserFound_ShouldFail()
    {
        // Arrange
        _userManagerMock.Setup(t => t.FindByIdAsync(It.IsAny<string>()))
            .ReturnsAsync(null as ApplicationUser);

        var claims = new Claim[]
        {
            new Claim(ClaimTypes.NameIdentifier, Guid.NewGuid().ToString())
        };
        var identity = new ClaimsIdentity(claims, "TestAuthType");
        var claimsPrincipal = new ClaimsPrincipal(identity);
        
        _contextAccessorMock.Setup(t => t.HttpContext.User)
            .Returns(claimsPrincipal);
        
        
        // Act
        var actual = await _getCurrentUserService.GetCurrUserObjectAsync();
        _output.WriteLine($"Actual UserObject:\n{actual?.Value?.ToString() ?? "null"}");
        
        // Assert
        actual.Should().NotBeNull();
        actual.IsSuccess.Should().BeFalse();
        actual.Value.Should().BeNull();
    }

    #endregion
}