using AutoFixture;
using FluentAssertions;
using HR_System.Core.Abstraction.ServiceContracts;
using HR_System.Core.Constraints;
using HR_System.Core.DTO.Token;
using HR_System.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using Moq;
using Xunit.Abstractions;

namespace HR_System.Core.UnitTests.ServicesTests;

public class CookiesServicesTests : ServiceTestBase
{
    private readonly ICookiesServices _cookiesServices;
    private readonly Mock<IHttpContextAccessor> _httpContextAccessorMock;
    private readonly Mock<IOptions<HR_System.Core.Options.CookieKeysOptions>> _cookieKeysMock;

    public CookiesServicesTests(ITestOutputHelper output) : base(output)
    {
        _httpContextAccessorMock = new Mock<IHttpContextAccessor>();
        _cookieKeysMock = new Mock<IOptions<HR_System.Core.Options.CookieKeysOptions>>();
        var memoryData = new Dictionary<string, string>()
        {
            {
                "Jwt:RefreshTokenLifeTime",
                "10080"
            },
            {
                "Jwt:AccessTokenLifeTime",
                "15"
            }
        };
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(memoryData!).Build();
        _cookiesServices = new CookiesServices(_httpContextAccessorMock.Object, Microsoft.Extensions.Options.Options.Create(new HR_System.Core.Options.JWTOptions { AccessTokenLifeTime = 15, RefreshTokenLifeTime = 10080 }), _cookieKeysMock.Object);
    }

    #region AddTests
    [Fact]
    public void Add_ValidData_ShouldSuccess()
    {
        // Arrange
        LogArrange("Scenario", nameof(Add_ValidData_ShouldSuccess));

        _httpContextAccessorMock.Setup(t => t.HttpContext!.Response.Cookies.Append(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CookieOptions>()));

        // Act
        var actual = _cookiesServices.Add("Dummy Key", "Dummy Data", 10);
        Output.WriteLine($"Actual: {actual}");
        LogAct(actual);

        // Assert
        actual.Should().NotBeNull();
        actual.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public void Add_NoContext_ShouldFail()
    {
        // Arrange
        LogArrange("Scenario", nameof(Add_NoContext_ShouldFail));

        _httpContextAccessorMock.Setup(t => t.HttpContext)
            .Returns(null as HttpContext);

        // Act
        var actual = _cookiesServices.Add("Dummy Key", "Dummy Data", 10);
        Output.WriteLine($"Actual: {actual.ToString()}");
        LogAct(actual);

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
        LogArrange("Scenario", nameof(AddTokens_ValidData_ShouldSuccess));

        _httpContextAccessorMock.Setup(t => t.HttpContext!.Response.Cookies.Append(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CookieOptions>()));
        _cookieKeysMock.Setup(t => t.Value)
            .Returns(new HR_System.Core.Options.CookieKeysOptions() { AccessToken = "AccessToken", RefreshToken = "RefreshToken" });

        // Act
        var result = _cookiesServices.AddTokens(Fixture.Create<AccessAndRefreshTokenDTO>());
        LogAct(result);

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
        LogArrange("Scenario", nameof(GetValue_ValidData_ShouldSucceed));

        var expected = Guid.NewGuid().ToString();
        _httpContextAccessorMock.Setup(t => t.HttpContext!.Request.Cookies.TryGetValue(It.IsAny<string>(), out expected))
            .Returns(true);
        Output.WriteLine($"Expected: {expected}");

        // Act
        var actual = _cookiesServices.GetValue<Guid>("dummy key");
        Output.WriteLine($"Actual: {actual.ToString()}");
        LogAct(actual);

        // Assert
        actual.Should().NotBeNull();
        actual.IsSuccess.Should().BeTrue();
        actual.Value.ToString().Should().Be(expected);
    }

    [Fact]
    public void GetValue_BadDataType_ShouldFail()
    {
        // Arrange
        LogArrange("Scenario", nameof(GetValue_BadDataType_ShouldFail));

        var badData = "bad Guid";
        _httpContextAccessorMock.Setup(t => t.HttpContext!.Request.Cookies.TryGetValue(It.IsAny<string>(), out badData))
            .Returns(true);
        Output.WriteLine($"Expected: {badData}");

        // Act
        var actual = _cookiesServices.GetValue<Guid>("dummy key");
        Output.WriteLine($"Actual: {actual.ToString()}");
        LogAct(actual);

        // Assert
        actual.Should().NotBeNull();
        actual.IsSuccess.Should().BeFalse();
    }
    #endregion
}
