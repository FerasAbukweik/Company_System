using System.Linq.Expressions;
using System.Net;
using AutoFixture;
using HR_System.Core.Abstraction.RepositoryContracts;
using HR_System.Core.Abstraction.ServiceContracts;
using HR_System.Core.Domain.Entities;
using HR_System.Core.Domain.Identity;
using HR_System.Core.DTO.Auth;
using HR_System.Core.DTO.Token;
using HR_System.Core.Enums;
using HR_System.Core.Result;
using HR_System.Infrastructure.Services;
using Microsoft.AspNetCore.Identity;
using Moq;

namespace HR_System.Core.UnitTests.ServicesTests;

public class AuthServiceTests : ServiceTestBase
{
    private readonly IAuthService _authService;
    private readonly Mock<UserManager<ApplicationUser>> _userManagerMock;
    private readonly Mock<IApplicationUsersRepository> _usersRepositoryMock;
    private readonly Mock<ICookiesServices> _cookiesServicesMock;
    private readonly Mock<ITokensService> _tokensServiceMock;

    public AuthServiceTests(ITestOutputHelper output) : base(output)
    {
        _userManagerMock = new Mock<UserManager<ApplicationUser>>(Mock.Of<IUserStore<ApplicationUser>>(), null !, null !, null !, null !, null !, null !, null !, null !);
        _usersRepositoryMock = new Mock<IApplicationUsersRepository>();
        _cookiesServicesMock = new Mock<ICookiesServices>();
        _tokensServiceMock = new Mock<ITokensService>();
        _authService = new AuthService(_userManagerMock.Object, _usersRepositoryMock.Object, _cookiesServicesMock.Object, _tokensServiceMock.Object);
    }

    // ---------- helpers ----------
    private ApplicationUser CreateUser() => new()
    {
        FullName = Fixture.Create<string>(),
        UserName = "employee",
        Email = "employee@example.com",
        Position = PositionsEnum.Employee,
        OrganizationHierarchy = new OrganizationHierarchy
        {
            UserId = Guid.NewGuid(),
            IsActive = true
        }
    };
    private void SetupUser(ApplicationUser? user)
    {
        _usersRepositoryMock.Setup(repository => repository.GetByEmailAsync(
            It.IsAny<string>(),
            It.IsAny<Expression<Func<ApplicationUser, object?>>[]?>(),
            It.IsAny<CancellationToken>()
        ))
            .ReturnsAsync(user);
    }

    #region LoginAsync
    [Fact]
    public async Task LoginAsync_ShouldReturnUnauthorized_WhenUserDoesNotExist()
    {
        var data = Fixture.Create<LoginDTO>();
        LogArrange(nameof(data.Email), data.Email);

        SetupUser(null);

        // Act
        var result = await _authService.LoginAsync(data);
        LogAct(result);

        // Assert
        result.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        _userManagerMock.Verify(
            manager => manager.CheckPasswordAsync(It.IsAny<ApplicationUser>(), It.IsAny<string>()),
            Times.Never
        );
    }

    [Theory]
    [InlineData(false, false, HttpStatusCode.Forbidden)]
    [InlineData(true, true, HttpStatusCode.Locked)]
    public async Task LoginAsync_ShouldRejectInactiveOrLockedAccountsBeforeCheckingPasswords(bool active, bool locked, HttpStatusCode status)
    {
        var user = CreateUser();
        user.OrganizationHierarchy.IsActive = active;
        SetupUser(user);
        _userManagerMock.Setup(manager => manager.IsLockedOutAsync(user))
            .ReturnsAsync(locked);
        LogArrange(nameof(user.Id), user.Id);

        // Act
        var result = await _authService.LoginAsync(Fixture.Create<LoginDTO>());
        LogAct(result);

        // Assert
        result.StatusCode.Should().Be(status);
        _userManagerMock.Verify(
            manager => manager.CheckPasswordAsync(It.IsAny<ApplicationUser>(), It.IsAny<string>()),
            Times.Never
        );
    }

    [Fact]
    public async Task LoginAsync_ShouldRecordFailedAttempts_WhenPasswordIsIncorrect()
    {
        var user = CreateUser();
        SetupUser(user);
        _userManagerMock.Setup(manager => manager.CheckPasswordAsync(user, It.IsAny<string>()))
            .ReturnsAsync(false);
        LogArrange(nameof(user.Id), user.Id);

        // Act
        var result = await _authService.LoginAsync(Fixture.Create<LoginDTO>());
        LogAct(result);

        // Assert
        result.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        _userManagerMock.Verify(manager => manager.AccessFailedAsync(user), Times.Once);
        _tokensServiceMock.VerifyNoOtherCalls();
    }

    [Theory]
    [InlineData(false, false)]
    [InlineData(true, false)]
    [InlineData(true, true)]
    public async Task LoginAsync_ShouldPropagateTokenAndCookieFailuresOrReturnMappedUser(bool tokenSuccess, bool cookieSuccess)
    {
        var user = CreateUser();
        var tokens = Fixture.Create<AccessAndRefreshTokenDTO>();
        SetupUser(user);
        _userManagerMock.Setup(manager => manager.CheckPasswordAsync(user, It.IsAny<string>()))
            .ReturnsAsync(true);
        _tokensServiceMock.Setup(service => service.GenerateNewTokensAsync(user, It.IsAny<CancellationToken>()))
            .ReturnsAsync(tokenSuccess ? Result<AccessAndRefreshTokenDTO>.Success(tokens) : Result<AccessAndRefreshTokenDTO>.Failure("Token failure"));
        _cookiesServicesMock.Setup(service => service.AddTokens(tokens))
            .Returns(cookieSuccess ? Result.Result.Success() : Result.Result.Failure("Cookie failure"));
        LogArrange(nameof(user.Id), user.Id);

        // Act
        var result = await _authService.LoginAsync(Fixture.Create<LoginDTO>());
        LogAct(result);

        // Assert
        result.IsSuccess.Should().Be(tokenSuccess && cookieSuccess);
        _userManagerMock.Verify(manager => manager.ResetAccessFailedCountAsync(user), Times.Once);
        if (result.IsSuccess)
            result.Value!.UserId.Should().Be(user.Id.ToString());
        else
            result.ErrorMessage.Should().Be(tokenSuccess ? "Cookie failure" : "Token failure");
    }

    #endregion
    #region Logout
    [Fact]
    public void Logout_ShouldRemoveCookiesAndSucceed()
    {
        // Arrange
        LogArrange("Scenario", nameof(Logout_ShouldRemoveCookiesAndSucceed));

        _cookiesServicesMock.Setup(service => service.RemoveTokens())
            .Returns(Result<string>.Success("Removed"));

        // Act
        var result = _authService.Logout();
        LogAct(result);

        // Assert
        result.IsSuccess.Should().BeTrue();
        _cookiesServicesMock.Verify(service => service.RemoveTokens(), Times.Once);
    }
    #endregion
}
