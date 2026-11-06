using HR_System.Core.DTO.LazyLoading;
using System.Net;
using HR_System.Controllers;
using HR_System.Core.Abstraction.RepositoryContracts;
using HR_System.Core.Domain.Entities;
using HR_System.Core.Domain.Identity;
using HR_System.Core.Enums;
using HR_System.Infrastructure;
using HR_System.Infrastructure.Repositories;
using HR_System.Infrastructure.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Moq;

namespace HR_System.Core.UnitTests.Integration;

public class AttendanceDayPageTests : ServiceTestBase, IDisposable
{
    private readonly ApplicationDbContext _context = new(new DbContextOptionsBuilder<ApplicationDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);

    public AttendanceDayPageTests(ITestOutputHelper output) : base(output)
    {
    }

    // ---------- DayRecords ----------
    #region DayRecords
    [Fact]
    public async Task DayRecords_ShouldReturnEmployeeNamesAndMatchingDayRecords()
    {
        // Arrange
        var first = new ApplicationUser
        {
            FullName = "First Employee",
            Position = PositionsEnum.Employee
        };
        var second = new ApplicationUser
        {
            FullName = "Second Employee",
            Position = PositionsEnum.Employee
        };
        var date = new DateOnly(2026, 11, 5);
        var unrecorded = new ApplicationUser { FullName = "Unrecorded Employee", Position = PositionsEnum.Employee };
        _context.AddRange(first, second, unrecorded);
        _context.AddRange(
            new DailyWorkRecord { UserId = first.Id, Date = date, State = WorkDayStateEnum.PaidLeave, MinutesWorked = 0 },
            new DailyWorkRecord { UserId = first.Id, Date = date.AddDays(-1), State = WorkDayStateEnum.Worked, MinutesWorked = 480 },
            new DailyWorkRecord { UserId = second.Id, Date = date, State = WorkDayStateEnum.Worked, MinutesWorked = 480 }
        );
        await _context.SaveChangesAsync();
        _context.ChangeTracker.Clear();
        var users = new Mock<IApplicationUsersRepository>(MockBehavior.Strict);
        var service = new DailyWorkRecordsService(new DailyWorkRecordsRepository(_context), users.Object);
        LogArrange(nameof(date), date);

        // Act
        var result = await service.GetDayRecordsAsync(date, new LazyDTO { Taken = 0, SectionSize = 5 });
        LogAct(result);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value!.TotalCount.Should().Be(3);
        result.Value.Items.Should().HaveCount(3);
        result.Value.Items.Select(employee => employee.FullName)
            .Should().BeEquivalentTo([first.FullName, second.FullName, unrecorded.FullName]);
        result.Value.Items.Single(employee => employee.UserId == unrecorded.Id).Record.Should().BeNull();
        var record = result.Value.Items.Single(employee => employee.UserId == first.Id).Record!;
        record.Date.Should().Be(date);
        record.MinutesWorked.Should().Be(0);
        record.State.Should().Be(WorkDayStateEnum.PaidLeave);
        users.VerifyNoOtherCalls();
        _context.ChangeTracker.Entries().Should().BeEmpty();
    }

    [Fact]
    public async Task DayRecords_ShouldReturnEmpty_WhenNoRecordsExistForTheDate()
    {
        // Arrange
        var date = new DateOnly(2026, 11, 5);
        var users = new Mock<IApplicationUsersRepository>(MockBehavior.Strict);
        var service = new DailyWorkRecordsService(new DailyWorkRecordsRepository(_context), users.Object);
        LogArrange(nameof(date), date);

        // Act
        var result = await service.GetDayRecordsAsync(date, new LazyDTO { Taken = 0, SectionSize = 5 });
        LogAct(result);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value!.Items.Should().BeEmpty();
        result.Value.TotalCount.Should().Be(0);
        users.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task DayRecords_ShouldRejectInvalidDate()
    {
        // Arrange
        var records = new Mock<IDailyWorkRecordsRepository>(MockBehavior.Strict);
        var service = new DailyWorkRecordsService(records.Object, Mock.Of<IApplicationUsersRepository>());
        LogArrange("Date", default(DateOnly));

        // Act
        var result = await service.GetDayRecordsAsync(default, new LazyDTO { Taken = 0, SectionSize = 5 });
        LogAct(result);

        // Assert
        result.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        records.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task DayRecords_ShouldPageNewestEmployees_AndKeepTotalCount()
    {
        // Arrange
        var date = new DateOnly(2026, 11, 5);
        _context.Users.AddRange(Enumerable.Range(1, 6).Select(index => new ApplicationUser
        {
            FullName = $"Employee {index}",
            CreatedAt = new DateTime(2026, 1, index),
            Position = PositionsEnum.Employee
        }));
        await _context.SaveChangesAsync();
        _context.ChangeTracker.Clear();
        var service = new DailyWorkRecordsService(new DailyWorkRecordsRepository(_context), Mock.Of<IApplicationUsersRepository>());
        var paging = new LazyDTO { Taken = 2, SectionSize = 2 };
        LogArrange(nameof(paging), paging);

        // Act
        var result = await service.GetDayRecordsAsync(date, paging);
        LogAct(result);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value!.TotalCount.Should().Be(6);
        result.Value.Items.Select(employee => employee.FullName).Should().Equal("Employee 4", "Employee 3");
        result.Value.Items.Should().OnlyContain(employee => employee.Record == null);
        _context.ChangeTracker.Entries().Should().BeEmpty();
    }

    #endregion
    // ---------- DayRecordsEndpoint ----------
    #region DayRecordsEndpoint
    [Fact]
    public void DayRecordsEndpoint_ShouldRequireAdmin()
    {
        // Arrange
        LogArrange("Scenario", nameof(DayRecordsEndpoint_ShouldRequireAdmin));

        var action = typeof(DailyWorkRecordsController).GetMethod(nameof(DailyWorkRecordsController.GetDayRecords))!;

        // Act
        var authorization = action.GetCustomAttributes(typeof(AuthorizeAttribute), true).Cast<AuthorizeAttribute>().Should().ContainSingle().Which;
        LogAct(authorization);

        // Assert
        authorization.Roles.Should().Be(nameof(RolesEnum.Admin));
    }

    public void Dispose() => _context.Dispose();
    #endregion
}
