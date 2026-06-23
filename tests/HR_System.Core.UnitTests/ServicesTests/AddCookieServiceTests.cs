using AutoFixture;
using FluentAssertions;
using HR_System.Core.DTO;
using HR_System.Core.Helpers;
using HR_System.Core.ServiceContracts.ICookieServices;
using HR_System.Core.ServiceContracts.ITokenServices;
using HR_System.Core.Services.CookiesService;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Moq;
using Xunit.Abstractions;

namespace HR_System.Core.UnitTests.ServicesTests;

public class AddCookieServiceTests
{
    private readonly IAddCookieService _addCookieService;
    private readonly ITestOutputHelper _output;
    private readonly IFixture _fixture;
    private readonly Mock<IHttpContextAccessor>  _httpContextAccessorMock;
    private readonly Mock<IGenerateTokenService> _generateTokenServiceMock;

    public AddCookieServiceTests(ITestOutputHelper output)
    {
        _output = output;
        
        _fixture = new Fixture();
        _fixture.Behaviors.OfType<ThrowingRecursionBehavior>().ToList()
            .ForEach(e => _fixture.Behaviors.Remove(e));
        _fixture.Behaviors.Add(new OmitOnRecursionBehavior());
        
        _httpContextAccessorMock = new Mock<IHttpContextAccessor>();
        _generateTokenServiceMock = new Mock<IGenerateTokenService>();

        var memoryData = new Dictionary<string, string>()
        {
            { "Jwt:RefreshTokenLifeTime", "10080" },
            { "Jwt:AccessTokenLifeTime", "15" }
        };
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(memoryData)
            .Build();
        
        _addCookieService = new AddCookieService(_httpContextAccessorMock.Object, _generateTokenServiceMock.Object, configuration);
    }


    #region AddTokensToCookiesTests

    [Fact]
    public async Task AddTokensToCookiesTest_ValidData_ShouldSuccess()
    {
        // Arrange
        var expected =  _fixture.Create<AccessAndRefreshTokenDTO>();
        
        _generateTokenServiceMock.Setup(t => t.GenerateNewAccessAndRefreshToken())
            .ReturnsAsync(Result<AccessAndRefreshTokenDTO>.Success(expected));
        
        // Act
        var actual = await _addCookieService.AddTokensToCookies();
        _output.WriteLine($"actual: {actual.ToString()}");

        // Assert
        actual.Should().NotBeNull();
        actual.IsSuccess.Should().BeTrue();
        actual.Value.Should().NotBeNull();
        actual.Value.Should().Be(expected);
    }
    
    [Fact]
    public async Task AddTokensToCookiesTest_NoTokens_ShouldFail()
    {
        // Arrange
        _generateTokenServiceMock.Setup(t => t.GenerateNewAccessAndRefreshToken())
            .ReturnsAsync(Result<AccessAndRefreshTokenDTO>.Failure("for testing"));
        
        // Act
        var actual = await _addCookieService.AddTokensToCookies();
        _output.WriteLine($"actual: {actual.ToString()}");

        // Assert
        actual.Should().NotBeNull();
        actual.IsSuccess.Should().BeFalse();
        actual.Value.Should().BeNull();
    }

    #endregion
}