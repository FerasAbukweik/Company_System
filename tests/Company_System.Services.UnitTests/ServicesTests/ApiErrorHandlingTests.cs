using System.Reflection;
using HR_System.Controllers;
using HR_System.MiddleWares;
using HR_System.SignalR.App;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging.Abstractions;

namespace HR_System.Core.UnitTests.ServicesTests;

public class ApiErrorHandlingTests : ServiceTestBase
{
    public ApiErrorHandlingTests(ITestOutputHelper output) : base(output)
    {
    }

    // ---------- AppHub ----------
    #region AppHub
    [Fact]
    public void AppHub_RequiresAuthentication()
    {
        // Arrange
        LogArrange("Scenario", nameof(AppHub_RequiresAuthentication));

        LogAct("Operation completed");

        // Act

        // Assert
        typeof(AppHub).GetCustomAttribute<AuthorizeAttribute>().Should().NotBeNull();
    }

    #endregion
    // ---------- Logout ----------
    #region Logout
    [Fact]
    public void Logout_AllowsExpiredSessionsToClearCookies()
    {
        // Arrange
        LogArrange("Scenario", nameof(Logout_AllowsExpiredSessionsToClearCookies));

        // Act
        var method = typeof(AuthController).GetMethod(nameof(AuthController.Logout))!;
        LogAct(method);

        // Assert
        method.GetCustomAttribute<AllowAnonymousAttribute>().Should().NotBeNull();
    }

    #endregion
    // ---------- UnexpectedErrors ----------
    #region UnexpectedErrors
    [Fact]
    public async Task UnexpectedErrors_DoNotExposeInternalExceptionDetails()
    {
        // Arrange
        LogArrange("Scenario", nameof(UnexpectedErrors_DoNotExposeInternalExceptionDetails));

        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();
        var middleware = new GlobalExceptionMiddleWare(_ => throw new InvalidOperationException("Private connection details"), NullLogger<GlobalExceptionMiddleWare>.Instance);
        await middleware.InvokeAsync(context);
        context.Response.Body.Position = 0;

        // Act
        var response = await new StreamReader(context.Response.Body).ReadToEndAsync();
        LogAct(response);

        // Assert
        context.Response.StatusCode.Should().Be(StatusCodes.Status500InternalServerError);
        response.Should().NotContain("Private connection details");
        response.Should().Contain("unexpected error");
    }
    #endregion
}
