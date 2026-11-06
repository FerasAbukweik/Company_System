using System.Net;
using HR_System.Core.Constraints;
using HR_System.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;

namespace HR_System.Core.UnitTests.ServicesTests;

public class CookieRemovalTests : ServiceTestBase
{
    public CookieRemovalTests(ITestOutputHelper output) : base(output)
    {
    }

    // ---------- Logout ----------
    #region Logout
    [Fact]
    public void Logout_ClearsRefreshCookieWhenAccessCookieIsAlreadyMissing()
    {
        // Arrange
        LogArrange("Scenario", nameof(Logout_ClearsRefreshCookieWhenAccessCookieIsAlreadyMissing));

        var context = new DefaultHttpContext();
        context.Request.Headers.Cookie = "refresh=still-valid";
        var service = new CookiesServices(new HttpContextAccessor { HttpContext = context }, Microsoft.Extensions.Options.Options.Create(new HR_System.Core.Options.JWTOptions()), Microsoft.Extensions.Options.Options.Create(new HR_System.Core.Options.CookieKeysOptions { AccessToken = "access", RefreshToken = "refresh" }));
        service.RemoveTokens();

        // Act
        var cookies = context.Response.Headers.SetCookie;
        LogAct(cookies);

        // Assert
        cookies.Should().Contain(value => value!.StartsWith("access="));
        cookies.Should().Contain(value => value!.StartsWith("refresh="));
    }

    #endregion
    // ---------- MissingCookie ----------
    #region MissingCookie
    [Fact]
    public void MissingCookie_ReturnsAnAuthenticationFailure()
    {
        // Arrange
        LogArrange("Scenario", nameof(MissingCookie_ReturnsAnAuthenticationFailure));

        var service = new CookiesServices(new HttpContextAccessor { HttpContext = new DefaultHttpContext() }, Microsoft.Extensions.Options.Options.Create(new HR_System.Core.Options.JWTOptions()), Microsoft.Extensions.Options.Options.Create(new HR_System.Core.Options.CookieKeysOptions()));

        // Act
        var result = service.Get("refresh");
        LogAct(result);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
    #endregion
}
