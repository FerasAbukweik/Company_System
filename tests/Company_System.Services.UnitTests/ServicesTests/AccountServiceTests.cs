using System.Linq.Expressions;
using AutoFixture;
using FluentAssertions;
using HR_System.Core.common;
using HR_System.Core.Domain.Identity;
using HR_System.Core.DTO.Auth;
using HR_System.Core.DTO.Token;
using HR_System.Core.Interfaces.RepositoryContracts;
using HR_System.Core.Interfaces.ServiceContracts.IAccountServices;
using HR_System.Core.Interfaces.ServiceContracts.ICookieServices;
using HR_System.Core.Interfaces.ServiceContracts.ITokenServices;
using HR_System.Infrastructure.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit.Abstractions;

namespace HR_System.Core.UnitTests.ServicesTests;

public class AccountServiceTests
{
    private readonly IAccountService _accountService;
    private readonly ITestOutputHelper _output;
    private readonly IFixture _fixture;
    private readonly Mock<UserManager<ApplicationUser>> _userManagerMock;
    private readonly Mock<IApplicationUsersRepository>  _userRepositoryMock;
    private readonly Mock<RoleManager<ApplicationRole>>  _roleManagerMock;
    private readonly Mock<ICookiesesServices> _cookieServiceMock;
    private readonly Mock<ITokenService> _tokenServiceMock;

    public AccountServiceTests(ITestOutputHelper output)
    {
        _output = output;

        _fixture = new Fixture();
        _fixture.Behaviors.OfType<ThrowingRecursionBehavior>().ToList()
            .ForEach(b => _fixture.Behaviors.Remove(b));
        _fixture.Behaviors.Add(new OmitOnRecursionBehavior());
        
        var userStore = new Mock<IUserStore<ApplicationUser>>();
        _userManagerMock = new Mock<UserManager<ApplicationUser>>(userStore.Object, null!, null!, null!, null!, null!, null!, null!, null!);

        var roleStore = new Mock<IRoleStore<ApplicationRole>>();
        _roleManagerMock = new Mock<RoleManager<ApplicationRole>>(roleStore.Object, null!, null!, null!, null!);
        
        _userRepositoryMock = new Mock<IApplicationUsersRepository>();
        _cookieServiceMock = new Mock<ICookiesesServices>();
        _tokenServiceMock = new Mock<ITokenService>();

        _accountService = new AccountService(_userManagerMock.Object,
            _userRepositoryMock.Object,
            _cookieServiceMock.Object,
            NullLogger<AccountService>.Instance,
            _tokenServiceMock.Object
        );
    }

    #region CreateAccountTests

    [Fact]
    public async Task CreateAccountAsync_ValidData_ShouldSuccess()
    {
        // Arrange
        var toCreate = _fixture.Create<AccountCreateDTO>();

        _userRepositoryMock.Setup(t => t.FilterAsync(It.IsAny<Expression<Func<ApplicationUser, bool>>>()))
            .ReturnsAsync([]);

        var transactionMock = new Mock<IDbContextTransaction>();
        transactionMock.Setup(t => t.CommitAsync())
            .Returns(Task.CompletedTask);
        
        _userRepositoryMock.Setup(t => t.BeginTransactionAsync())
            .ReturnsAsync(transactionMock.Object);

        _userManagerMock.Setup(t => t.CreateAsync(It.IsAny<ApplicationUser>(), It.IsAny<string>()))
            .ReturnsAsync(IdentityResult.Success);

        _roleManagerMock.Setup(t => t.RoleExistsAsync(It.IsAny<string>()))
            .ReturnsAsync(true);
        
        _userManagerMock.Setup(t => t.AddToRoleAsync(It.IsAny<ApplicationUser>(), It.IsAny<string>()))
            .ReturnsAsync(IdentityResult.Success);
        
        _tokenServiceMock.Setup(t => t.GenerateNewAccessAndRefreshTokenAsync(It.IsAny<ApplicationUser>()))
            .ReturnsAsync(Result<AccessAndRefreshTokenDTO>.Success(_fixture.Create<AccessAndRefreshTokenDTO>()));

        _cookieServiceMock.Setup(t => t.AddTokens(It.IsAny<AccessAndRefreshTokenDTO>()));
        _output.WriteLine($"Expected:\n{toCreate.ToString()}");

        // Act
        var actual = await _accountService.CreateAccountAsync(toCreate);
        _output.WriteLine($"Actual:\n{actual.Value?.ToString() ?? "null"}");

        // Assert
        actual.Should().NotBeNull();
        actual.IsSuccess.Should().BeTrue();
        actual.Value.Should().NotBeNull();
        actual.Value.Email.Should().Be(toCreate.Email);
        actual.Value.PhoneNumber.Should().Be(toCreate.PhoneNumber);
        actual.Value.UserName.Should().Be(toCreate.UserName);
        actual.Value.FullName.Should().Be(toCreate.FullName);
        actual.Value.Id.Should().NotBe(Guid.Empty);
        transactionMock.Verify(t => t.CommitAsync(It.IsAny<CancellationToken>()), Times.Once);
    }
    
    [Fact]
    public async Task CreateAccountAsync_WhenUserAlreadyExists_ShouldReturnFailure()
    {
        var toCreate = _fixture.Create<AccountCreateDTO>();
        var existingUser = _fixture.Build<ApplicationUser>()
            .With(u => u.Email, toCreate.Email)
            .Create();
        
        _userRepositoryMock.Setup(t => t.FilterAsync(It.IsAny<Expression<Func<ApplicationUser, bool>>>()))
            .ReturnsAsync([existingUser]);

        var actual = await _accountService.CreateAccountAsync(toCreate);

        actual.Should().NotBeNull();
        actual.IsSuccess.Should().BeFalse();
        _userManagerMock.Verify(t => t.CreateAsync(It.IsAny<ApplicationUser>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task CreateAccountAsync_WhenIdentityCreationFails_ShouldReturnFailure()
    {
        var toCreate = _fixture.Create<AccountCreateDTO>();

        _userRepositoryMock.Setup(t => t.FilterAsync(It.IsAny<Expression<Func<ApplicationUser, bool>>>()))
            .ReturnsAsync([]);

        var transactionMock = new Mock<IDbContextTransaction>();
        _userRepositoryMock.Setup(t => t.BeginTransactionAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(transactionMock.Object);

        var identityError = new IdentityError { Code = "PasswordTooShort", Description = "Password is too short" };
        var failedResult = IdentityResult.Failed(identityError);
        
        _userManagerMock.Setup(t => t.CreateAsync(It.IsAny<ApplicationUser>(), It.IsAny<string>()))
            .ReturnsAsync(failedResult);

        var actual = await _accountService.CreateAccountAsync(toCreate);

        actual.Should().NotBeNull();
        actual.IsSuccess.Should().BeFalse();
        transactionMock.Verify(t => t.CommitAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task CreateAccountAsync_WhenRoleDoesntExist_ShouldSuccess()
    {
        var toCreate = _fixture.Create<AccountCreateDTO>();

        _userRepositoryMock.Setup(t => t.FilterAsync(It.IsAny<Expression<Func<ApplicationUser, bool>>>()))
            .ReturnsAsync([]);

        var transactionMock = new Mock<IDbContextTransaction>();
        _userRepositoryMock.Setup(t => t.BeginTransactionAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(transactionMock.Object);

        _userManagerMock.Setup(t => t.CreateAsync(It.IsAny<ApplicationUser>(), It.IsAny<string>()))
            .ReturnsAsync(IdentityResult.Success);

        _userManagerMock.Setup(t => t.AddToRoleAsync(It.IsAny<ApplicationUser>(), It.IsAny<string>()))
            .ReturnsAsync(IdentityResult.Success);

        _roleManagerMock.Setup(t => t.RoleExistsAsync(It.IsAny<string>()))
            .ReturnsAsync(false);

        _roleManagerMock.Setup(t => t.CreateAsync(It.IsAny<ApplicationRole>()))
            .ReturnsAsync(IdentityResult.Success);

        _cookieServiceMock.Setup(t => t.AddTokens(It.IsAny<AccessAndRefreshTokenDTO>()));

        _tokenServiceMock.Setup(t => t.GenerateNewAccessAndRefreshTokenAsync(It.IsAny<ApplicationUser>()))
            .ReturnsAsync(Result<AccessAndRefreshTokenDTO>.Success(_fixture.Create<AccessAndRefreshTokenDTO>()));

        var actual = await _accountService.CreateAccountAsync(toCreate);

        actual.Should().NotBeNull();
        actual.IsSuccess.Should().BeTrue();
        actual.Value.Should().NotBeNull();
        actual.Value.Should().BeAssignableTo<ApplicationUser>();
        transactionMock.Verify(t => t.CommitAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CreateAccountAsync_WhenAddToRoleFails_ShouldReturnFailure()
    {
        var toCreate = _fixture.Create<AccountCreateDTO>();

        _userRepositoryMock.Setup(t => t.FilterAsync(It.IsAny<Expression<Func<ApplicationUser, bool>>>()))
            .ReturnsAsync([]);

        var transactionMock = new Mock<IDbContextTransaction>();
        _userRepositoryMock.Setup(t => t.BeginTransactionAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(transactionMock.Object);

        _userManagerMock.Setup(t => t.CreateAsync(It.IsAny<ApplicationUser>(), It.IsAny<string>()))
            .ReturnsAsync(IdentityResult.Success);

        _roleManagerMock.Setup(t => t.RoleExistsAsync(It.IsAny<string>()))
            .ReturnsAsync(true);

        var failedRoleResult = IdentityResult.Failed(new IdentityError { Description = "Failed to assign role" });
        _userManagerMock.Setup(t => t.AddToRoleAsync(It.IsAny<ApplicationUser>(), It.IsAny<string>()))
            .ReturnsAsync(failedRoleResult);

        var actual = await _accountService.CreateAccountAsync(toCreate);

        actual.Should().NotBeNull();
        actual.IsSuccess.Should().BeFalse();
        transactionMock.Verify(t => t.CommitAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    #endregion
    
}