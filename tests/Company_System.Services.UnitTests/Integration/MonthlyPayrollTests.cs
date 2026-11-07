using System.Net;
using HR_System.Controllers;
using HR_System.Core.Abstraction.RepositoryContracts;
using HR_System.Core.Abstraction.ServiceContracts;
using HR_System.Core.Domain.Entities;
using HR_System.Core.Domain.Identity;
using HR_System.Core.Enums;
using HR_System.Core.Result;
using HR_System.Infrastructure;
using HR_System.Infrastructure.BackGroundServices;
using HR_System.Infrastructure.Decorators.Loggers;
using HR_System.Infrastructure.Repositories;
using HR_System.Infrastructure.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace HR_System.Core.UnitTests.Integration;

public class MonthlyPayrollTests : ServiceTestBase, IDisposable
{
    private readonly ApplicationDbContext _context = new(new DbContextOptionsBuilder<ApplicationDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);

    public MonthlyPayrollTests(ITestOutputHelper output) : base(output)
    {
    }

    private PaymentsService CreateService() => new(new PaymentsRepository(_context), new ApplicationUsersRepository(_context, new UpperInvariantLookupNormalizer()), new DailyWorkRecordsRepository(_context));
    private ApplicationUser Employee(decimal? hourlyRate, bool active = true)
    {
        var user = new ApplicationUser
        {
            FullName = "Employee",
            Position = PositionsEnum.Employee,
            HourlyRate = hourlyRate
        };
        user.OrganizationHierarchy = new OrganizationHierarchy
        {
            UserId = user.Id,
            IsActive = active
        };
        _context.Add(user);
        return user;
    }

    // ---------- Generation ----------
    #region Generation
    [Fact]
    public async Task Generation_ShouldUseClockTimesAndOnlyActiveConfiguredEmployees()
    {
        // Arrange
        LogArrange("Scenario", nameof(Generation_ShouldUseClockTimesAndOnlyActiveConfiguredEmployees));

        var employee = Employee(200.10m);
        Employee(null);
        Employee(0);
        Employee(12000, active: false);
        Employee(0.01m);
        _context.Add(new DailyWorkRecord { UserId = employee.Id, Date = new DateOnly(2026, 11, 5), ClockIn = new TimeOnly(8, 0), ClockOut = new TimeOnly(8, 0).AddMinutes(450), State = WorkDayStateEnum.Worked });
        _context.Add(new DailyWorkRecord { UserId = employee.Id, Date = new DateOnly(2026, 12, 1), ClockIn = new TimeOnly(8, 0), ClockOut = new TimeOnly(8, 0).AddMinutes(480), State = WorkDayStateEnum.Worked });
        await _context.SaveChangesAsync();
        _context.ChangeTracker.Clear();

        // Act
        var result = await CreateService().GenerateMonthlyPaymentsAsync(new DateOnly(2026, 11, 1));
        LogAct(result);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(1);
        var payment = await _context.Payments.AsNoTracking().SingleAsync();
        payment.UserId.Should().Be(employee.Id);
        payment.PaidAmount.Should().Be(1500.75m);
        payment.State.Should().Be(PaymentStateEnum.Pending);
        payment.PaymentDate.Should().BeNull();
        payment.Year.Should().Be(2026);
        payment.Month.Should().Be(11);
    }

    [Fact]
    public async Task Generation_ShouldRoundOnceAfterSummingDurationsAndIgnoreOtherMonths()
    {
        // Arrange
        LogArrange("Scenario", nameof(Generation_ShouldRoundOnceAfterSummingDurationsAndIgnoreOtherMonths));

        var employee = Employee(0.30m);
        _context.AddRange(
            new DailyWorkRecord { UserId = employee.Id, Date = new DateOnly(2026, 10, 31), ClockIn = new TimeOnly(8, 0), ClockOut = new TimeOnly(8, 0).AddMinutes(480), State = WorkDayStateEnum.Worked },
            new DailyWorkRecord { UserId = employee.Id, Date = new DateOnly(2026, 11, 1), ClockIn = new TimeOnly(8, 0), ClockOut = new TimeOnly(8, 0).AddMinutes(1), State = WorkDayStateEnum.Worked },
            new DailyWorkRecord { UserId = employee.Id, Date = new DateOnly(2026, 11, 2), ClockIn = new TimeOnly(8, 0), ClockOut = new TimeOnly(8, 0).AddMinutes(1), State = WorkDayStateEnum.Worked }
        );
        await _context.SaveChangesAsync();
        _context.ChangeTracker.Clear();

        // Act
        var result = await CreateService().GenerateMonthlyPaymentsAsync(new DateOnly(2026, 11, 1));
        LogAct(result);

        // Assert
        result.Value.Should().Be(1);
        (await _context.Payments.SingleAsync()).PaidAmount.Should().Be(0.01m);
    }

    [Fact]
    public async Task Generation_ShouldIgnoreIncompleteRecordsAndUseElapsedTimeAcrossMidnight()
    {
        // Arrange
        var employee = Employee(20);
        var incomplete = Employee(20);
        var clockIn = new TimeOnly(22, 0);
        _context.AddRange(
            new DailyWorkRecord { UserId = employee.Id, Date = new DateOnly(2026, 11, 30), ClockIn = clockIn, ClockOut = clockIn.AddHours(8), State = WorkDayStateEnum.Worked },
            new DailyWorkRecord { UserId = employee.Id, Date = new DateOnly(2026, 11, 29), ClockIn = clockIn, ClockOut = null, State = WorkDayStateEnum.Worked },
            new DailyWorkRecord { UserId = incomplete.Id, Date = new DateOnly(2026, 11, 28), ClockIn = null, ClockOut = clockIn, State = WorkDayStateEnum.Worked },
            new DailyWorkRecord { UserId = incomplete.Id, Date = new DateOnly(2026, 11, 29), ClockIn = clockIn, ClockOut = null, State = WorkDayStateEnum.Worked },
            new DailyWorkRecord { UserId = incomplete.Id, Date = new DateOnly(2026, 11, 30), State = WorkDayStateEnum.Holiday }
        );
        await _context.SaveChangesAsync();
        _context.ChangeTracker.Clear();
        LogArrange(nameof(clockIn), clockIn);

        // Act
        var result = await CreateService().GenerateMonthlyPaymentsAsync(new DateOnly(2026, 11, 1));
        LogAct(result);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(1);
        var payment = await _context.Payments.SingleAsync();
        payment.UserId.Should().Be(employee.Id);
        payment.PaidAmount.Should().Be(160);
    }

    #endregion
    // ---------- Restart ----------
    #region Restart
    [Fact]
    public async Task Restart_ShouldOnlyCreateMissingRecordsAndPreserveDecisionsAndAmounts()
    {
        // Arrange
        LogArrange("Scenario", nameof(Restart_ShouldOnlyCreateMissingRecordsAndPreserveDecisionsAndAmounts));

        var existing = Employee(12000);
        var missing = Employee(20);
        _context.Add(new DailyWorkRecord { UserId = missing.Id, Date = new DateOnly(2026, 11, 5), ClockIn = new TimeOnly(8, 0), ClockOut = new TimeOnly(8, 0).AddMinutes(450), State = WorkDayStateEnum.Worked });
        _context.Add(new Payment { UserId = existing.Id, Year = 2026, Month = 11, PaidAmount = 750, PaymentDate = new DateOnly(2026, 11, 1), State = PaymentStateEnum.Rejected });
        await _context.SaveChangesAsync();
        _context.ChangeTracker.Clear();

        // Act
        var first = await CreateService().GenerateMonthlyPaymentsAsync(new DateOnly(2026, 11, 1));
        _context.ChangeTracker.Clear();
        var second = await CreateService().GenerateMonthlyPaymentsAsync(new DateOnly(2026, 11, 1));
        LogAct(first);

        // Assert
        first.Value.Should().Be(1);
        second.Value.Should().Be(0);
        var records = await _context.Payments.AsNoTracking().ToListAsync();
        records.Count.Should().Be(2);
        records.Single(payment => payment.UserId == existing.Id).PaidAmount.Should().Be(750);
        records.Single(payment => payment.UserId == existing.Id).State.Should().Be(PaymentStateEnum.Rejected);
        records.Single(payment => payment.UserId == missing.Id).PaidAmount.Should().Be(150);
    }

    #endregion
    // ---------- NextMonth ----------
    #region NextMonth
    [Fact]
    public async Task NextMonth_ShouldGenerateANewPeriodWithoutChangingPreviousMonths()
    {
        // Arrange
        LogArrange("Scenario", nameof(NextMonth_ShouldGenerateANewPeriodWithoutChangingPreviousMonths));

        var employee = Employee(20);
        _context.AddRange(
            new DailyWorkRecord { UserId = employee.Id, Date = new DateOnly(2026, 12, 31), ClockIn = new TimeOnly(8, 0), ClockOut = new TimeOnly(8, 0).AddMinutes(480), State = WorkDayStateEnum.Worked },
            new DailyWorkRecord { UserId = employee.Id, Date = new DateOnly(2027, 1, 1), ClockIn = new TimeOnly(8, 0), ClockOut = new TimeOnly(8, 0).AddMinutes(480), State = WorkDayStateEnum.Worked }
        );
        await _context.SaveChangesAsync();
        _context.ChangeTracker.Clear();
        await CreateService().GenerateMonthlyPaymentsAsync(new DateOnly(2026, 12, 1));
        _context.ChangeTracker.Clear();

        // Act
        var next = await CreateService().GenerateMonthlyPaymentsAsync(new DateOnly(2027, 1, 1));
        LogAct(next);

        // Assert
        next.Value.Should().Be(1);
        (await _context.Payments.CountAsync()).Should().Be(2);
    }

    #endregion
    // ---------- Generation ----------
    #region Generation
    [Fact]
    public async Task Generation_ShouldRejectNonMonthlyPeriodsWithoutQuerying()
    {
        // Arrange
        LogArrange("Scenario", nameof(Generation_ShouldRejectNonMonthlyPeriodsWithoutQuerying));

        var repository = new Mock<IPaymentsRepository>();
        var users = new Mock<IApplicationUsersRepository>();

        // Act
        var result = await new PaymentsService(repository.Object, users.Object, Mock.Of<IDailyWorkRecordsRepository>()).GenerateMonthlyPaymentsAsync(new DateOnly(2026, 11, 5));
        LogAct(result);

        // Assert
        result.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        repository.VerifyNoOtherCalls();
        users.VerifyNoOtherCalls();
    }

    #endregion
    // ---------- Worker ----------
    #region Worker
    [Fact]
    public async Task Worker_ShouldGenerateOnceOnStartupAndStopWhileWaitingForNextMonth()
    {
        // Arrange
        LogArrange("Scenario", nameof(Worker_ShouldGenerateOnceOnStartupAndStopWhileWaitingForNextMonth));

        var completed = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var payments = new Mock<IPaymentsService>();
        payments.Setup(service => service.GenerateMonthlyPaymentsAsync(It.IsAny<DateOnly>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<int>.Success(0))
            .Callback(() => completed.TrySetResult());
        var services = new ServiceCollection();
        services.AddScoped(_ => payments.Object);
        await using var provider = services.BuildServiceProvider();
        using var worker = new GenerateMonthlyPayments(provider.GetRequiredService<IServiceScopeFactory>(), NullLogger<GenerateMonthlyPayments>.Instance);
        await worker.StartAsync(CancellationToken.None);
        await completed.Task.WaitAsync(TimeSpan.FromSeconds(5));

        // Act
        await worker.StopAsync(CancellationToken.None);
        LogAct("Operation completed");

        // Assert
        payments.Verify(
            service => service.GenerateMonthlyPaymentsAsync(It.Is<DateOnly>(date => date.Day == 1), It.IsAny<CancellationToken>()),
            Times.Once
        );
    }

    [Fact]
    public async Task Worker_ShouldHandleFailureWithoutStoppingTheHost()
    {
        // Arrange
        LogArrange("Scenario", nameof(Worker_ShouldHandleFailureWithoutStoppingTheHost));

        var attempted = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var payments = new Mock<IPaymentsService>();
        payments.Setup(service => service.GenerateMonthlyPaymentsAsync(It.IsAny<DateOnly>(), It.IsAny<CancellationToken>())).ThrowsAsync(new InvalidOperationException("Database unavailable"))
            .Callback(() => attempted.TrySetResult());
        var services = new ServiceCollection();
        services.AddScoped(_ => payments.Object);
        await using var provider = services.BuildServiceProvider();
        using var worker = new GenerateMonthlyPayments(provider.GetRequiredService<IServiceScopeFactory>(), NullLogger<GenerateMonthlyPayments>.Instance);
        await worker.StartAsync(CancellationToken.None);

        // Act
        await attempted.Task.WaitAsync(TimeSpan.FromSeconds(5));
        LogAct("Operation completed");

        // Assert
        worker.ExecuteTask!.IsCompleted.Should().BeFalse();
        await worker.StopAsync(CancellationToken.None);
    }

    #endregion
    // ---------- Scheduling ----------
    #region Scheduling
    [Theory]
    [InlineData("2026-11-30T23:59:59+00:00", 2026, 11)]
    [InlineData("2026-12-01T00:00:00+00:00", 2026, 12)]
    [InlineData("2026-12-31T23:59:59+00:00", 2026, 12)]
    [InlineData("2027-01-01T00:00:00+00:00", 2027, 1)]
    public void Scheduling_ShouldUseUtcMonthBoundaries(string utc, int year, int month)
    {
        // Arrange
        var now = DateTimeOffset.Parse(utc).UtcDateTime;
        var period = GenerateMonthlyPayments.GetPayrollMonth(now);
        LogArrange(nameof(utc), utc);

        // Act
        var next = GenerateMonthlyPayments.GetNextRunAt(now);
        LogAct(next);

        // Assert
        period.Should().Be(new DateOnly(year, month, 1).AddMonths(-1));
        next.Day.Should().Be(1);
        next.Hour.Should().Be(0);
        next.Kind.Should().Be(DateTimeKind.Utc);
        DateOnly.FromDateTime(next).Should().Be(period.AddMonths(2));
    }

    #endregion
    // ---------- Logger ----------
    #region Logger
    [Fact]
    public async Task Logger_ShouldForwardMonthlyGeneration()
    {
        // Arrange
        LogArrange("Scenario", nameof(Logger_ShouldForwardMonthlyGeneration));

        var inner = new Mock<IPaymentsService>();
        var month = new DateOnly(2026, 11, 1);
        var expected = Result<int>.Success(5);
        using var cancellation = new CancellationTokenSource();
        inner.Setup(service => service.GenerateMonthlyPaymentsAsync(month, cancellation.Token))
            .ReturnsAsync(expected);

        // Act
        var logger = new LoggingPaymentsService(inner.Object, NullLogger<LoggingPaymentsService>.Instance);
        LogAct(logger);

        // Assert
        (await logger.GenerateMonthlyPaymentsAsync(month, cancellation.Token)).Should().BeSameAs(expected);
    }

    #endregion
    // ---------- PaymentController ----------
    #region PaymentController
    [Fact]
    public void PaymentController_ShouldExposeNoManualCreationEndpoint()
    {
        // Arrange
        LogArrange("Scenario", nameof(PaymentController_ShouldExposeNoManualCreationEndpoint));

        LogAct("Operation completed");

        // Act

        // Assert
        typeof(PaymentsController).GetMethods().Should().NotContain(method => method.GetCustomAttributes(typeof(HttpPostAttribute), true).Length > 0);
        typeof(IPaymentsService).GetMethods().Should().NotContain(method => method.Name == "AddAsync");
    }

    public void Dispose()
    {
        _context.Dispose();
    }
    #endregion
}
