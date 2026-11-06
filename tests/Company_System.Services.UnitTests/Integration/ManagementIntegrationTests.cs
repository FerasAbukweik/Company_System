using HR_System.Core.DTO.Auth;
using System.Linq.Expressions;
using System.Net;
using System.Security.Claims;
using HR_System.Controllers;
using HR_System.Core.Abstraction.RepositoryContracts;
using HR_System.Core.Abstraction.ServiceContracts;
using HR_System.Core.Domain.Entities;
using HR_System.Core.Domain.Identity;
using HR_System.Core.DTO.Payments;
using HR_System.Core.DTO.Users;
using HR_System.Core.Enums;
using HR_System.Core.Result;
using HR_System.Infrastructure;
using HR_System.Infrastructure.Decorators.Caching;
using HR_System.Infrastructure.Repositories;
using HR_System.Infrastructure.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Moq;

namespace HR_System.Core.UnitTests.Integration;

public class ManagementIntegrationTests : ServiceTestBase, IDisposable
{
    private readonly DbContextOptions<ApplicationDbContext> _options = new DbContextOptionsBuilder<ApplicationDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options;
    private readonly ApplicationDbContext _context;

    public ManagementIntegrationTests(ITestOutputHelper output) : base(output)
    {
        _context = new ApplicationDbContext(_options);
    }

    private static Payment Payment(Guid userId, int month, PaymentStateEnum state = PaymentStateEnum.Pending) => new()
    {
        UserId = userId,
        Year = 2026,
        Month = month,
        PaidAmount = 1200.50m,
        PaymentDate = new DateOnly(2026, month, 25),
        State = state
    };
    // ---------- Approval ----------
    #region Approval
    [Fact]
    public async Task Approval_ShouldSetPaymentDateAndPreserveItOnRepeatedApproval()
    {
        // Arrange
        LogArrange("Scenario", nameof(Approval_ShouldSetPaymentDateAndPreserveItOnRepeatedApproval));

        var payment = Payment(Guid.NewGuid(), 1);
        payment.PaymentDate = null;
        _context.Add(payment);
        await _context.SaveChangesAsync();
        _context.ChangeTracker.Clear();
        var service = new PaymentsService(new PaymentsRepository(_context), Mock.Of<IApplicationUsersRepository>(), Mock.Of<IDailyWorkRecordsRepository>());
        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        // Act
        var approved = await service.UpdateStateAsync(payment.Id, PaymentStateEnum.Approved);
        LogAct(approved);

        // Assert
        approved.Value!.PaymentDate.Should().Be(today);
        _context.ChangeTracker.Clear();
        var stored = await _context.Payments.FindAsync(payment.Id);
        stored!.PaymentDate = today.AddDays(-3);
        await _context.SaveChangesAsync();
        _context.ChangeTracker.Clear();
        var repeated = await service.UpdateStateAsync(payment.Id, PaymentStateEnum.Approved);
        repeated.Value!.PaymentDate.Should().Be(today.AddDays(-3));
    }

    #endregion
    // ---------- NonApprovedStates ----------
    #region NonApprovedStates
    [Theory]
    [InlineData(PaymentStateEnum.Pending)]
    [InlineData(PaymentStateEnum.Rejected)]
    public async Task NonApprovedStates_ShouldClearPaymentDate(PaymentStateEnum state)
    {
        // Arrange
        var payment = Payment(Guid.NewGuid(), 1, PaymentStateEnum.Approved);
        _context.Add(payment);
        await _context.SaveChangesAsync();
        _context.ChangeTracker.Clear();
        var service = new PaymentsService(new PaymentsRepository(_context), Mock.Of<IApplicationUsersRepository>(), Mock.Of<IDailyWorkRecordsRepository>());
        LogArrange(nameof(state), state);

        // Act
        var result = await service.UpdateStateAsync(payment.Id, state);
        LogAct(result);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value!.PaymentDate.Should().BeNull();
        (await _context.Payments.AsNoTracking().SingleAsync()).PaymentDate.Should().BeNull();
    }

    #endregion
    // ---------- BatchDecisions ----------
    #region BatchDecisions
    [Theory]
    [InlineData(PaymentStateEnum.Approved)]
    [InlineData(PaymentStateEnum.Rejected)]
    public async Task BatchDecisions_ShouldSaveMatchingPaymentDates(PaymentStateEnum state)
    {
        // Arrange
        var first = Payment(Guid.NewGuid(), 1);
        var second = Payment(Guid.NewGuid(), 1);
        first.PaymentDate = second.PaymentDate = null;
        _context.AddRange(first, second);
        await _context.SaveChangesAsync();
        _context.ChangeTracker.Clear();
        var service = new PaymentsService(new PaymentsRepository(_context), Mock.Of<IApplicationUsersRepository>(), Mock.Of<IDailyWorkRecordsRepository>());
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        LogArrange(nameof(state), state);

        // Act
        var result = await service.UpdateStatesAsync([first.Id, second.Id], state);
        LogAct(result);

        // Assert
        result.IsSuccess.Should().BeTrue();
        (await _context.Payments.AsNoTracking().ToListAsync()).Should().AllSatisfy(record =>
        {
            record.State.Should().Be(state);
            record.PaymentDate.Should().Be(state == PaymentStateEnum.Approved ? today : (DateOnly? )null);
        });
    }

    #endregion
    // ---------- AdminListing ----------
    #region AdminListing
    [Fact]
    public async Task AdminListing_ShouldReturnRealEmployeeNamesAndApplyStateAndPaging()
    {
        // Arrange
        LogArrange("Scenario", nameof(AdminListing_ShouldReturnRealEmployeeNamesAndApplyStateAndPaging));

        var user = new ApplicationUser
        {
            FullName = "Actual Employee",
            Position = PositionsEnum.Employee
        };
        _context.Add(user);
        _context.AddRange(Payment(user.Id, 1), Payment(user.Id, 2), Payment(user.Id, 3, PaymentStateEnum.Approved));
        await _context.SaveChangesAsync();
        _context.ChangeTracker.Clear();
        var service = new PaymentsService(new PaymentsRepository(_context), Mock.Of<IApplicationUsersRepository>(), Mock.Of<IDailyWorkRecordsRepository>());

        // Act
        var result = await service.GetPaymentsAsync(new PaymentQueryDTO { Year = 2026, State = PaymentStateEnum.Pending, Taken = 1, SectionSize = 1 });
        LogAct(result);

        // Assert
        result.IsSuccess.Should().BeTrue();
        var item = result.Value!.Items.Should().ContainSingle().Which;
        item.EmployeeName.Should().Be("Actual Employee");
        item.State.Should().Be(PaymentStateEnum.Pending);
        _context.ChangeTracker.Entries().Should().BeEmpty();
    }

    #endregion
    // ---------- BatchDecision ----------
    #region BatchDecision
    [Fact]
    public async Task BatchDecision_ShouldSaveAllSelectedPendingRecordsAndLeaveOthersUntouched()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var first = Payment(userId, 1);
        var second = Payment(userId, 2);
        var history = Payment(userId, 3, PaymentStateEnum.Rejected);
        _context.AddRange(first, second, history);
        await _context.SaveChangesAsync();
        _context.ChangeTracker.Clear();
        var service = new PaymentsService(new PaymentsRepository(_context), Mock.Of<IApplicationUsersRepository>(), Mock.Of<IDailyWorkRecordsRepository>());
        LogArrange(nameof(userId), userId);

        // Act
        var result = await service.UpdateStatesAsync([first.Id, second.Id], PaymentStateEnum.Approved);
        LogAct(result);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value!.Count.Should().Be(2);
        (await _context.Payments.CountAsync(p => p.State == PaymentStateEnum.Approved)).Should().Be(2);
        (await _context.Payments.AsNoTracking().SingleAsync(p => p.Id == history.Id)).State.Should().Be(PaymentStateEnum.Rejected);
    }

    [Fact]
    public async Task BatchDecision_ShouldRejectTheWholeBatchWhenOneRecordIsAlreadyDecided()
    {
        // Arrange
        LogArrange("Scenario", nameof(BatchDecision_ShouldRejectTheWholeBatchWhenOneRecordIsAlreadyDecided));

        var first = Payment(Guid.NewGuid(), 1);
        var second = Payment(Guid.NewGuid(), 2, PaymentStateEnum.Approved);
        _context.AddRange(first, second);
        await _context.SaveChangesAsync();
        _context.ChangeTracker.Clear();
        var service = new PaymentsService(new PaymentsRepository(_context), Mock.Of<IApplicationUsersRepository>(), Mock.Of<IDailyWorkRecordsRepository>());

        // Act
        var result = await service.UpdateStatesAsync([first.Id, second.Id], PaymentStateEnum.Rejected);
        LogAct(result);

        // Assert
        result.StatusCode.Should().Be(HttpStatusCode.Conflict);
        _context.ChangeTracker.Entries().Should().BeEmpty();
        (await _context.Payments.AsNoTracking().SingleAsync(p => p.Id == first.Id)).State.Should().Be(PaymentStateEnum.Pending);
    }

    #endregion
    // ---------- ConcurrentDecision ----------
    #region ConcurrentDecision
    [Fact]
    public async Task ConcurrentDecision_ShouldNotOverwriteAnotherAdminsDecision()
    {
        // Arrange
        LogArrange("Scenario", nameof(ConcurrentDecision_ShouldNotOverwriteAnotherAdminsDecision));

        var payment = Payment(Guid.NewGuid(), 1);
        _context.Add(payment);
        await _context.SaveChangesAsync();
        _context.ChangeTracker.Clear();
        var firstRepository = new PaymentsRepository(_context);
        var original = await firstRepository.GetByIdAsync(payment.Id);
        using var otherContext = new ApplicationDbContext(_options);
        var otherRepository = new PaymentsRepository(otherContext);
        await otherRepository.UpdateStateAsync(payment.Id, PaymentStateEnum.Approved);
        await otherRepository.SaveChangesAsync();

        // Act
        firstRepository.SetState(original!, PaymentStateEnum.Rejected);
        LogAct("Operation completed");

        // Assert
        await FluentActions.Awaiting(() => firstRepository.SaveChangesAsync()).Should().ThrowAsync<DbUpdateConcurrencyException>();
    }

    #endregion
    // ---------- AdminListing ----------
    #region AdminListing
    [Theory]
    [InlineData(0, 100)]
    [InlineData(2026, 0)]
    public async Task AdminListing_ShouldValidateFilters(int year, int size)
    {
        // Arrange
        var repository = new Mock<IPaymentsRepository>();
        var service = new PaymentsService(repository.Object, Mock.Of<IApplicationUsersRepository>(), Mock.Of<IDailyWorkRecordsRepository>());
        LogArrange(nameof(year), year);

        // Act
        var result = await service.GetPaymentsAsync(new PaymentQueryDTO { Year = year, SectionSize = size });
        LogAct(result);

        // Assert
        result.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        repository.VerifyNoOtherCalls();
    }

    #endregion
    // ---------- ProfileEndpoint ----------
    #region ProfileEndpoint
    [Fact]
    public async Task ProfileEndpoint_ShouldOnlyRequestTheAuthenticatedEmployee()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var service = new Mock<IUsersService>();
        service.Setup(s => s.GetUserAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<UserDTO>.Failure("Not found", HttpStatusCode.NotFound));
        var controller = new UsersController(service.Object)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext
                {
                    User = new ClaimsPrincipal(new ClaimsIdentity([new Claim(ClaimTypes.NameIdentifier, userId.ToString())]))
                }
            }
        };
        LogArrange(nameof(userId), userId);

        // Act
        await controller.Me();
        LogAct("Operation completed");

        // Assert
        service.Verify(s => s.GetUserAsync(userId, It.IsAny<CancellationToken>()), Times.Once);
    }

    #endregion
    // ---------- EmployeeUpdate ----------
    #region EmployeeUpdate
    [Fact]
    public async Task EmployeeUpdate_ShouldPersistHourlyRateAndHolidayConfigurationThroughIdentity()
    {
        // Arrange
        LogArrange("Scenario", nameof(EmployeeUpdate_ShouldPersistHourlyRateAndHolidayConfigurationThroughIdentity));

        var user = new ApplicationUser
        {
            FullName = "Employee",
            Position = PositionsEnum.Employee,
            UserName = "employee",
            Email = "employee@example.com"
        };
        var repository = new Mock<IApplicationUsersRepository>();
        repository.Setup(r => r.GetByIdAsync(
            user.Id,
            It.IsAny<Expression<Func<ApplicationUser, object?>>[]>(),
            It.IsAny<CancellationToken>()
        ))
            .ReturnsAsync(user);
        repository.Setup(r => r.FilterAsync(
            It.IsAny<Expression<Func<ApplicationUser, bool>>>(),
            null,
            null,
            false,
            null,
            1,
            It.IsAny<CancellationToken>()
        ))
            .ReturnsAsync(Array.Empty<ApplicationUser>());
        var manager = new Mock<UserManager<ApplicationUser>>(Mock.Of<IUserStore<ApplicationUser>>(), null !, null !, null !, null !, null !, null !, null !, null !);
        manager.Setup(m => m.UpdateAsync(user))
            .ReturnsAsync(IdentityResult.Success);
        var service = new UsersService(manager.Object, repository.Object, Mock.Of<IOrganizationHierarchyService>(), Mock.Of<IImageService>(), Mock.Of<IOrganizationHierarchyRepository>());

        // Act
        var result = await service.UpdateEmployeeAsync(
            user.Id,
            new EmployeeUpdateDTO { FullName = "Updated Employee", UserName = "employee", Email = "employee@example.com", PhoneNumber = "12345678", Position = PositionsEnum.Employee, HourlyRate = 48000, AnnualHolidayAllowance = 22 }
        );
        LogAct(result);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value!.HourlyRate.Should().Be(48000);
        result.Value.AnnualHolidayAllowance.Should().Be(22);
        manager.Verify(m => m.UpdateAsync(user), Times.Once);
    }

    #endregion
    // ---------- ImageReplacement ----------
    #region ImageReplacement
    [Fact]
    public async Task ImageReplacement_ShouldRetainTheOldImageAsAnOrphanForCleanup()
    {
        // Arrange
        LogArrange("Scenario", nameof(ImageReplacement_ShouldRetainTheOldImageAsAnOrphanForCleanup));

        var user = new ApplicationUser
        {
            FullName = "Employee",
            Position = PositionsEnum.Employee
        };
        var old = new Image
        {
            PublicId = "old",
            Url = "https://example.com/old",
            UserId = user.Id
        };
        _context.AddRange(user, old);
        await _context.SaveChangesAsync();
        _context.ChangeTracker.Clear();
        var repository = new ApplicationUsersRepository(_context, new UpperInvariantLookupNormalizer());
        var replacement = new Image
        {
            PublicId = "new",
            Url = "https://example.com/new"
        };
        await repository.ReplaceImageAsync(user.Id, replacement);

        // Act
        await repository.SaveChangesAsync();
        LogAct("Operation completed");

        // Assert
        (await _context.Images.AsNoTracking().SingleAsync(i => i.Id == old.Id)).UserId.Should().BeNull();
        (await _context.Images.AsNoTracking().SingleAsync(i => i.Id == replacement.Id)).UserId.Should().Be(user.Id);
    }

    #endregion
    // ---------- ManagementEndpoints ----------
    #region ManagementEndpoints
    [Theory]
    [InlineData(typeof(UsersController), nameof(UsersController.UpdateEmployee))]
    [InlineData(typeof(UsersController), nameof(UsersController.Deactivate))]
    [InlineData(typeof(PaymentsController), nameof(PaymentsController.GetPayments))]
    [InlineData(typeof(PaymentsController), nameof(PaymentsController.UpdateStates))]
    public void ManagementEndpoints_ShouldRequireAdminRole(Type type, string name)
    {
        // Arrange
        LogArrange(nameof(type), type);

        // Act
        var attributes = type.GetMethod(name)!.GetCustomAttributes(typeof(AuthorizeAttribute), true).Cast<AuthorizeAttribute>();
        LogAct(attributes);

        // Assert
        attributes.Should().ContainSingle().Which.Roles.Should().Be(nameof(RolesEnum.Admin));
    }

    #endregion
    // ---------- EmployeeEdit ----------
    #region EmployeeEdit
    [Fact]
    public async Task EmployeeEdit_ShouldInvalidateTheOrgTreeCache()
    {
        // Arrange
        var inner = new Mock<IUsersService>();
        var redis = new Mock<IRedisService>();
        var id = Guid.NewGuid();
        var data = new EmployeeUpdateDTO
        {
            FullName = "Employee",
            UserName = "employee",
            Email = "employee@example.com",
            PhoneNumber = "12345678",
            Position = PositionsEnum.Employee
        };
        inner.Setup(service => service.UpdateEmployeeAsync(id, data, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<EmployeeDTO>.Success(new EmployeeDTO { Id = id, FullName = "Employee", UserName = "employee", Email = "employee@example.com", Phone = "12345678", Position = PositionsEnum.Employee, ImageUrl = null, IsActive = true }));
        var service = new InvalidatingUsersService(inner.Object, redis.Object);
        LogArrange(nameof(id), id);

        // Act
        var result = await service.UpdateEmployeeAsync(id, data);
        LogAct(result);

        // Assert
        result.IsSuccess.Should().BeTrue();
        redis.Verify(cache => cache.RemoveByPrefixAsync("hierarchy"), Times.Once);
    }

    #endregion
    // ---------- MigrationSnapshot ----------
    #region MigrationSnapshot
    [Fact]
    public void MigrationSnapshot_ShouldMatchTheCurrentModel()
    {
        // Arrange
        LogArrange("Scenario", nameof(MigrationSnapshot_ShouldMatchTheCurrentModel));

        // Act
        using var context = new ApplicationDbContext(new DbContextOptionsBuilder<ApplicationDbContext>().UseSqlServer("Server=localhost;Database=SchemaValidation;Trusted_Connection=True;TrustServerCertificate=True").Options);
        LogAct(context);

        // Assert
        context.Database.HasPendingModelChanges().Should().BeFalse();
    }

    public void Dispose()
    {
        _context.Dispose();
    }
    #endregion
}
