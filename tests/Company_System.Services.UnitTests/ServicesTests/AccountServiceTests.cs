using AutoFixture;
   using FluentAssertions;
   using HR_System.Core.common;
   using HR_System.Core.Domain.Identity;
   using HR_System.Core.DTO.Account;
   using HR_System.Core.DTO.Auth;
   using HR_System.Core.DTO.Token;
   using HR_System.Core.Enums;
   using HR_System.Core.Interfaces.RepositoryContracts;
   using HR_System.Core.Interfaces.ServiceContracts;
   using HR_System.Infrastructure.Services;
   using Microsoft.AspNetCore.Identity;
   using Microsoft.Extensions.Logging;
   using Moq;
   using Xunit.Abstractions;
   
   namespace TestProject1.ServicesTests;
   
   public class AccountServiceTests
   {
       private readonly IAccountService _accountService;
       private readonly Mock<UserManager<ApplicationUser>> _userManagerMock;
       private readonly Mock<IApplicationUsersRepository> _usersRepositoryMock;
       private readonly Mock<ICookiesServices> _cookiesServicesMock;
       private readonly Mock<ILogger<AccountService>> _loggerMock;
       private readonly Mock<ITokenService> _tokenServiceMock;
       private readonly ITestOutputHelper _output;
       private readonly IFixture _fixture;
   
       public AccountServiceTests(ITestOutputHelper output)
       {
           _output = output;
   
           _fixture = new Fixture();
           _fixture.Behaviors.OfType<ThrowingRecursionBehavior>().ToList()
               .ForEach(b => _fixture.Behaviors.Remove(b));
           _fixture.Behaviors.Add(new OmitOnRecursionBehavior());
   
           var storeMock = new Mock<IUserStore<ApplicationUser>>();
           _userManagerMock = new Mock<UserManager<ApplicationUser>>(
               storeMock.Object, null!, null!, null!, null!, null!, null!, null!, null!);
   
           _usersRepositoryMock = new Mock<IApplicationUsersRepository>();
           _cookiesServicesMock = new Mock<ICookiesServices>();
           _loggerMock = new Mock<ILogger<AccountService>>();
           _tokenServiceMock = new Mock<ITokenService>();
   
           _accountService = new AccountService(
               _userManagerMock.Object,
               _usersRepositoryMock.Object,
               _cookiesServicesMock.Object,
               _loggerMock.Object,
               _tokenServiceMock.Object);
       }
   
       private UserCreateDTO CreateUserCreateDto(PositionsEnum? position = null)
       {
           return _fixture.Build<UserCreateDTO>()
               .With(u => u.Position, position ?? PositionsEnum.Employee) // adjust if enum names differ
               .Create();
       }
   
       private LoginDTO CreateLoginDto()
       {
           return _fixture.Create<LoginDTO>();
       }
   
       private ApplicationUser CreateUser()
       {
           return _fixture.Build<ApplicationUser>()
               .Without(u => u.RefreshTokens)
               .Without(u => u.Tasks)
               .Without(u => u.CreatedTasks)
               .Without(u => u.Approvals)
               .Without(u => u.ToApprove)
               .Without(u => u.Activities)
               .Without(u => u.OrganizationHierarchy)
               .Without(u => u.SentMessages)
               .Without(u => u.ReceivedMessages)
               .Create();
       }
   
       private void SetupNoExistingUsers()
       {
           _usersRepositoryMock
               .Setup(r => r.FilterAsync(
                   It.IsAny<System.Linq.Expressions.Expression<Func<ApplicationUser, bool>>>(),
                   It.IsAny<System.Linq.Expressions.Expression<Func<ApplicationUser, object?>>[]?>(),
                   It.IsAny<CancellationToken>()))
               .ReturnsAsync(new List<ApplicationUser>());
       }
   
       #region CreateAccountAsync
   
       [Fact]
       public async Task CreateAccountAsync_ShouldReturnFailure_WhenPositionIsUnknown()
       {
           // Arrange
           var dto = CreateUserCreateDto(PositionsEnum.unknown);
   
           // Act
           var result = await _accountService.CreateAccountAsync(dto, null, null);
   
           // Assert
           result.IsSuccess.Should().BeFalse();
           result.ErrorMessage.Should().Be("Cannt Add user with unknown position");
   
           _usersRepositoryMock.Verify(
               r => r.FilterAsync(
                   It.IsAny<System.Linq.Expressions.Expression<Func<ApplicationUser, bool>>>(),
                   It.IsAny<System.Linq.Expressions.Expression<Func<ApplicationUser, object?>>[]?>(),
                   It.IsAny<CancellationToken>()),
               Times.Never);
       }
   
       [Fact]
       public async Task CreateAccountAsync_ShouldReturnFailure_WhenUserAlreadyExists()
       {
           // Arrange
           var dto = CreateUserCreateDto();
           var existingUser = CreateUser();
           existingUser.UserName = dto.UserName;
           existingUser.Email = dto.Email;
           existingUser.PhoneNumber = dto.PhoneNumber;
   
           _usersRepositoryMock
               .Setup(r => r.FilterAsync(
                   It.IsAny<System.Linq.Expressions.Expression<Func<ApplicationUser, bool>>>(),
                   It.IsAny<System.Linq.Expressions.Expression<Func<ApplicationUser, object?>>[]?>(),
                   It.IsAny<CancellationToken>()))
               .ReturnsAsync(new List<ApplicationUser> { existingUser });
   
           // Act
           var result = await _accountService.CreateAccountAsync(dto, null, null);
   
           // Assert
           result.IsSuccess.Should().BeFalse();
           _userManagerMock.Verify(
               m => m.CreateAsync(It.IsAny<ApplicationUser>(), It.IsAny<string>()),
               Times.Never);
       }
   
       [Fact]
       public async Task CreateAccountAsync_ShouldReturnFailure_WhenUserCreationFails()
       {
           // Arrange
           var dto = CreateUserCreateDto();
           SetupNoExistingUsers();
   
           _userManagerMock
               .Setup(m => m.CreateAsync(It.IsAny<ApplicationUser>(), dto.Password))
               .ReturnsAsync(IdentityResult.Failed(new IdentityError { Description = "weak password" }));
   
           // Act
           var result = await _accountService.CreateAccountAsync(dto, null, null);
   
           // Assert
           result.IsSuccess.Should().BeFalse();
           result.ErrorMessage.Should().Be("weak password");
   
           _userManagerMock.Verify(
               m => m.AddToRoleAsync(It.IsAny<ApplicationUser>(), It.IsAny<string>()),
               Times.Never);
       }
   
       [Fact]
       public async Task CreateAccountAsync_ShouldReturnFailure_WhenRoleAssignmentFails()
       {
           // Arrange
           var dto = CreateUserCreateDto();
           SetupNoExistingUsers();
   
           _userManagerMock
               .Setup(m => m.CreateAsync(It.IsAny<ApplicationUser>(), dto.Password))
               .ReturnsAsync(IdentityResult.Success);
   
           _userManagerMock
               .Setup(m => m.AddToRoleAsync(It.IsAny<ApplicationUser>(), It.IsAny<string>()))
               .ReturnsAsync(IdentityResult.Failed(new IdentityError { Description = "role assignment failed" }));
   
           // Act
           var result = await _accountService.CreateAccountAsync(dto, null, null);
   
           // Assert
           result.IsSuccess.Should().BeFalse();
           result.ErrorMessage.Should().Be("role assignment failed");
       }
   
       [Fact]
       public async Task CreateAccountAsync_ShouldAssignAdminRole_WhenPositionIsCeo()
       {
           // Arrange
           var dto = CreateUserCreateDto(PositionsEnum.CEO);
           SetupNoExistingUsers();
   
           _userManagerMock
               .Setup(m => m.CreateAsync(It.IsAny<ApplicationUser>(), dto.Password))
               .ReturnsAsync(IdentityResult.Success);
   
           _userManagerMock
               .Setup(m => m.AddToRoleAsync(It.IsAny<ApplicationUser>(), nameof(RolesEnum.Admin)))
               .ReturnsAsync(IdentityResult.Success);
   
           // Act
           var result = await _accountService.CreateAccountAsync(dto, null, null);
   
           // Assert
           result.IsSuccess.Should().BeTrue();
           _userManagerMock.Verify(
               m => m.AddToRoleAsync(It.IsAny<ApplicationUser>(), nameof(RolesEnum.Admin)),
               Times.Once);
       }
   
       [Fact]
       public async Task CreateAccountAsync_ShouldAssignEmployeeRole_WhenPositionIsNotCeo()
       {
           // Arrange
           var dto = CreateUserCreateDto(PositionsEnum.Employee);
           SetupNoExistingUsers();
   
           _userManagerMock
               .Setup(m => m.CreateAsync(It.IsAny<ApplicationUser>(), dto.Password))
               .ReturnsAsync(IdentityResult.Success);
   
           _userManagerMock
               .Setup(m => m.AddToRoleAsync(It.IsAny<ApplicationUser>(), nameof(RolesEnum.Employee)))
               .ReturnsAsync(IdentityResult.Success);
   
           // Act
           var result = await _accountService.CreateAccountAsync(dto, null, null);
   
           // Assert
           result.IsSuccess.Should().BeTrue();
           _userManagerMock.Verify(
               m => m.AddToRoleAsync(It.IsAny<ApplicationUser>(), nameof(RolesEnum.Employee)),
               Times.Once);
       }
   
       [Fact]
       public async Task CreateAccountAsync_ShouldReturnSuccessWithMappedUser_WhenAllStepsSucceed()
       {
           // Arrange
           var dto = CreateUserCreateDto();
           SetupNoExistingUsers();
   
           _userManagerMock
               .Setup(m => m.CreateAsync(It.IsAny<ApplicationUser>(), dto.Password))
               .ReturnsAsync(IdentityResult.Success);
   
           _userManagerMock
               .Setup(m => m.AddToRoleAsync(It.IsAny<ApplicationUser>(), It.IsAny<string>()))
               .ReturnsAsync(IdentityResult.Success);
   
           // Act
           var result = await _accountService.CreateAccountAsync(dto, "http://image.url", "image-id");
   
           // Assert
           result.IsSuccess.Should().BeTrue();
           result.Value.Should().NotBeNull();
           result.Value!.UserName.Should().Be(dto.UserName);
           result.Value.Email.Should().Be(dto.Email);
           result.Value.PhoneNumber.Should().Be(dto.PhoneNumber);
           result.Value.FullName.Should().Be(dto.FullName);
           result.Value.Position.Should().Be(dto.Position);
           result.Value.ImageUrl.Should().Be("http://image.url");
           result.Value.PublicImageId.Should().Be("image-id");
       }
   
       #endregion
   
       #region LoginAsync
   
       [Fact]
       public async Task LoginAsync_ShouldReturnFailure_WhenUserNotFound()
       {
           // Arrange
           var dto = CreateLoginDto();
   
           _userManagerMock
               .Setup(m => m.FindByEmailAsync(dto.Email))
               .ReturnsAsync((ApplicationUser?)null);
   
           // Act
           var result = await _accountService.LoginAsync(dto);
   
           // Assert
           result.IsSuccess.Should().BeFalse();
           result.ErrorMessage.Should().Be("Invalid Email or Password");
       }
   
       [Fact]
       public async Task LoginAsync_ShouldReturnFailure_WhenPasswordIsIncorrect()
       {
           // Arrange
           var dto = CreateLoginDto();
           var user = CreateUser();
   
           _userManagerMock
               .Setup(m => m.FindByEmailAsync(dto.Email))
               .ReturnsAsync(user);
   
           _userManagerMock
               .Setup(m => m.CheckPasswordAsync(user, dto.Password))
               .ReturnsAsync(false);
   
           // Act
           var result = await _accountService.LoginAsync(dto);
   
           // Assert
           result.IsSuccess.Should().BeFalse();
           result.ErrorMessage.Should().Be("Invalid Email or Password");
   
           _tokenServiceMock.Verify(
               t => t.GenerateNewTokensAsync(It.IsAny<ApplicationUser>(), It.IsAny<CancellationToken>()),
               Times.Never);
       }
   
       [Fact]
       public async Task LoginAsync_ShouldReturnFailure_WhenTokenGenerationFails()
       {
           // Arrange
           var dto = CreateLoginDto();
           var user = CreateUser();
   
           _userManagerMock.Setup(m => m.FindByEmailAsync(dto.Email)).ReturnsAsync(user);
           _userManagerMock.Setup(m => m.CheckPasswordAsync(user, dto.Password)).ReturnsAsync(true);
   
           _tokenServiceMock
               .Setup(t => t.GenerateNewTokensAsync(user, It.IsAny<CancellationToken>()))
               .ReturnsAsync(Result<AccessAndRefreshTokenDTO>.Failure("token generation failed"));
   
           // Act
           var result = await _accountService.LoginAsync(dto);
   
           // Assert
           result.IsSuccess.Should().BeFalse();
           result.ErrorMessage.Should().Be("token generation failed");
   
           _cookiesServicesMock.Verify(
               c => c.AddTokens(It.IsAny<AccessAndRefreshTokenDTO>()),
               Times.Never);
       }
   
       [Fact]
       public async Task LoginAsync_ShouldReturnFailure_WhenAddingTokensToCookiesFails()
       {
           // Arrange
           var dto = CreateLoginDto();
           var user = CreateUser();
           var tokens = _fixture.Create<AccessAndRefreshTokenDTO>();
   
           _userManagerMock.Setup(m => m.FindByEmailAsync(dto.Email)).ReturnsAsync(user);
           _userManagerMock.Setup(m => m.CheckPasswordAsync(user, dto.Password)).ReturnsAsync(true);
   
           _tokenServiceMock
               .Setup(t => t.GenerateNewTokensAsync(user, It.IsAny<CancellationToken>()))
               .ReturnsAsync(Result<AccessAndRefreshTokenDTO>.Success(tokens));
   
           _cookiesServicesMock
               .Setup(c => c.AddTokens(tokens))
               .Returns(Result.Failure("failed to set cookies"));
   
           // Act
           var result = await _accountService.LoginAsync(dto);
   
           // Assert
           result.IsSuccess.Should().BeFalse();
           result.ErrorMessage.Should().Be("failed to set cookies");
       }
   
       [Fact]
       public async Task LoginAsync_ShouldReturnSuccessWithMappedUser_WhenAllStepsSucceed()
       {
           // Arrange
           var dto = CreateLoginDto();
           var user = CreateUser();
           var tokens = _fixture.Create<AccessAndRefreshTokenDTO>();
   
           _userManagerMock.Setup(m => m.FindByEmailAsync(dto.Email)).ReturnsAsync(user);
           _userManagerMock.Setup(m => m.CheckPasswordAsync(user, dto.Password)).ReturnsAsync(true);
   
           _tokenServiceMock
               .Setup(t => t.GenerateNewTokensAsync(user, It.IsAny<CancellationToken>()))
               .ReturnsAsync(Result<AccessAndRefreshTokenDTO>.Success(tokens));
   
           _cookiesServicesMock
               .Setup(c => c.AddTokens(tokens))
               .Returns(Result.Success());
   
           // Act
           var result = await _accountService.LoginAsync(dto);
   
           // Assert
           result.IsSuccess.Should().BeTrue();
           result.Value.Should().NotBeNull();
           result.Value!.UserId.Should().Be(user.Id.ToString());
           result.Value.UserName.Should().Be(user.UserName);
       }
   
       #endregion
   }