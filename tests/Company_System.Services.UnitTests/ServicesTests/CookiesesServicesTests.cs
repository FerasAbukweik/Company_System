using AutoFixture;
using FluentAssertions;
using HR_System.Core.DTO;
using HR_System.Core.DTO.Token;
using HR_System.Core.Interfaces.ServiceContracts.ICookieServices;
using HR_System.Infrastructure.Services;
using HR_System.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit.Abstractions;

namespace HR_System.Core.UnitTests.ServicesTests;

public class CookiesesServicesTests
{
    private readonly ICookiesesServices _cookiesesServices;
    private readonly ITestOutputHelper _output;
    private readonly IFixture _fixture;
    private readonly Mock<IHttpContextAccessor>  _httpContextAccessorMock;

    public CookiesesServicesTests(ITestOutputHelper output)
    {
        _output = output;
        
        _fixture = new Fixture();
        _fixture.Behaviors.OfType<ThrowingRecursionBehavior>().ToList()
            .ForEach(e => _fixture.Behaviors.Remove(e));
        _fixture.Behaviors.Add(new OmitOnRecursionBehavior());
        
        _httpContextAccessorMock = new Mock<IHttpContextAccessor>();

        var memoryData = new Dictionary<string, string>()
        {
            { "Jwt:RefreshTokenLifeTime", "10080" },
            { "Jwt:AccessTokenLifeTime", "15" }
        };
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(memoryData!)
            .Build();
        
        _cookiesesServices = new CookiesesServices(_httpContextAccessorMock.Object, configuration, NullLogger<CookiesesServices>.Instance);
    }


    #region AddTests

    [Fact]
    public void Add_ValidData_ShouldSuccess()
    {
        // Arrange
        _httpContextAccessorMock.Setup(t => t.HttpContext!.Response.Cookies.Append(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CookieOptions>()));
        
        // Act
        var actual =  _cookiesesServices.Add("Dummy Key","Dummy Data",10);
        _output.WriteLine($"Actual: {actual}");
        
        // Assert
        actual.Should().NotBeNull();
        actual.IsSuccess.Should().BeTrue();
    }
    
    [Fact]
    public void Add_NoContext_ShouldFail()
    {
        // Arrange
        _httpContextAccessorMock.Setup(t => t.HttpContext).Returns(null as HttpContext);
        
        // Act
        var actual =  _cookiesesServices.Add("Dummy Key","Dummy Data",10);
        _output.WriteLine($"Actual: {actual.ToString()}");
        
        // Assert
        actual.Should().NotBeNull();
        actual.IsSuccess.Should().BeFalse();
    }

    #endregion

    #region AddTokensTests

    [Fact]
    public void AddTokens_ValidData_ShouldSuccess()
    {
        // Arrange
        _httpContextAccessorMock.Setup(t => 
            t.HttpContext!.Response.Cookies.Append(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CookieOptions>()));
        
        // Act
        var result = _cookiesesServices.AddTokens(_fixture.Create<AccessAndRefreshTokenDTO>());
        
        // Assert
        result.Should().NotBeNull();
        result.IsSuccess.Should().BeTrue();
    }
    

    #endregion

    #region GetValue<T>Tests

    [Fact]
    public void GetValue_ValidData_ShouldSucceed()
    {
        // Arrange
        var expected = Guid.NewGuid().ToString();

        _httpContextAccessorMock.Setup(t =>
            t.HttpContext!.Request.Cookies.TryGetValue(It.IsAny<string>(), out expected))
            .Returns(true);
        
        _output.WriteLine($"Expected: {expected}");

        // Act
        var actual = _cookiesesServices.GetValue<Guid>("dummy key");
        _output.WriteLine($"Actual: {actual.ToString()}");

        // Assert
        actual.Should().NotBeNull();
        actual.IsSuccess.Should().BeTrue();
        actual.Value.ToString().Should().Be(expected);
    }
    
    [Fact]
    public void GetValue_BadDataType_ShouldFail()
    {
        // Arrange
        var badData = "bad Guid";

        _httpContextAccessorMock.Setup(t =>
                t.HttpContext!.Request.Cookies.TryGetValue(It.IsAny<string>(), out badData))
            .Returns(true);
        
        _output.WriteLine($"Expected: {badData}");

        // Act
        var actual = _cookiesesServices.GetValue<Guid>("dummy key");
        _output.WriteLine($"Actual: {actual.ToString()}");

        // Assert
        actual.Should().NotBeNull();
        actual.IsSuccess.Should().BeFalse();
    }

    #endregion
    
}