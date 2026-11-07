using HR_System.Core.Abstraction.RepositoryContracts;
using HR_System.Core.Domain.Entities;
using HR_System.Core.Domain.Identity;
using HR_System.Core.DTO.Payments;
using HR_System.Core.Enums;
using HR_System.Infrastructure;
using HR_System.Infrastructure.Repositories;
using HR_System.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Moq;

namespace HR_System.Core.UnitTests.Integration;

public class PortalPaymentPaginationTests : ServiceTestBase, IDisposable
{
    private readonly ApplicationDbContext _context = new(new DbContextOptionsBuilder<ApplicationDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);

    public PortalPaymentPaginationTests(ITestOutputHelper output) : base(output)
    {
    }

    // ---------- PaymentTables ----------
    #region PaymentTables
    [Fact]
    public async Task PaymentTables_ShouldOrderByNewestPayrollPeriodBeforePaging()
    {
        // Arrange
        var userId = Guid.NewGuid();
        _context.Add(new ApplicationUser { Id = userId, FullName = "Employee", Position = PositionsEnum.Employee });
        var highId = Guid.Parse("ffffffff-ffff-ffff-ffff-ffffffffffff");
        var lowId = Guid.Parse("00000000-0000-0000-0000-000000000001");
        _context.AddRange(
            new Payment { Id = Guid.NewGuid(), UserId = userId, Year = 2026, Month = 1, PaidAmount = 100, PaymentDate = new DateOnly(2026, 1, 1) },
            new Payment { Id = lowId, UserId = userId, Year = 2026, Month = 2, PaidAmount = 100, PaymentDate = new DateOnly(2026, 11, 1) },
            new Payment { Id = highId, UserId = userId, Year = 2026, Month = 3, PaidAmount = 100, PaymentDate = new DateOnly(2026, 11, 1) }
        );
        await _context.SaveChangesAsync();
        var service = new PaymentsService(new PaymentsRepository(_context), Mock.Of<IApplicationUsersRepository>(), Mock.Of<IDailyWorkRecordsRepository>());
        var admin = await service.GetPaymentsAsync(new PaymentQueryDTO { Year = 2026, Taken = 0, SectionSize = 1 });
        LogArrange(nameof(userId), userId);

        // Act
        var portal = await service.GetUserPaymentPageAsync(userId, new UserPaymentQueryDTO { Year = 2026, Taken = 1, SectionSize = 1 });
        LogAct(portal);

        // Assert
        admin.Value!.Items.Should().ContainSingle().Which.Id.Should().Be(highId);
        portal.Value!.Items.Should().ContainSingle().Which.Id.Should().Be(lowId);
    }

    #endregion
    // ---------- Pages ----------
    #region Pages
    [Fact]
    public async Task Pages_ShouldReturnOnlyTheFilteredUsersRowsAndCount()
    {
        // Arrange
        var userId = Guid.NewGuid();
        for (var month = 1; month <= 12; month++)
            _context.Add(new Payment { UserId = userId, Year = 2026, Month = month, PaidAmount = 100, PaymentDate = new DateOnly(2026, month, 1), State = month % 2 == 0 ? PaymentStateEnum.Approved : PaymentStateEnum.Pending });
        _context.Add(new Payment { UserId = Guid.NewGuid(), Year = 2026, Month = 1, PaidAmount = 9999, PaymentDate = new DateOnly(2026, 1, 1) });
        await _context.SaveChangesAsync();
        _context.ChangeTracker.Clear();
        var service = new PaymentsService(new PaymentsRepository(_context), Mock.Of<IApplicationUsersRepository>(), Mock.Of<IDailyWorkRecordsRepository>());
        LogArrange(nameof(userId), userId);

        // Act
        var first = await service.GetUserPaymentPageAsync(userId, new UserPaymentQueryDTO { Year = 2026, Taken = 0, SectionSize = 5 });
        var second = await service.GetUserPaymentPageAsync(userId, new UserPaymentQueryDTO { Year = 2026, Taken = 5, SectionSize = 5 });
        var all = await service.GetUserPaymentPageAsync(userId, new UserPaymentQueryDTO { Year = 2026, SectionSize = 1000 });
        LogAct(first);

        // Assert
        first.Value!.Items.Count.Should().Be(5);
        second.Value!.Items.Count.Should().Be(5);
        all.Value!.Items.Count.Should().Be(12);
        first.Value.Items.Select(payment => payment.Id).Intersect(second.Value.Items.Select(payment => payment.Id)).Should().BeEmpty();
        first.Value.TotalCount.Should().Be(12);
        second.Value.TotalCount.Should().Be(12);
        all.Value.Items.Should().AllSatisfy(payment => payment.UserId.Should().Be(userId));
    }

    [Fact]
    public async Task Overview_ShouldScopeCurrentYearTotalsAndHolidaysToTheUserAndReturnLatestApproved()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var otherUser = Guid.NewGuid();
        _context.Payments.AddRange(
            new Payment { UserId = userId, Year = today.Year, Month = 1, PaidAmount = 100, State = PaymentStateEnum.Approved, PaymentDate = today.AddDays(-2) },
            new Payment { UserId = userId, Year = today.Year, Month = 2, PaidAmount = 200, State = PaymentStateEnum.Pending },
            new Payment { UserId = userId, Year = today.Year - 1, Month = 12, PaidAmount = 300, State = PaymentStateEnum.Approved, PaymentDate = today },
            new Payment { UserId = otherUser, Year = today.Year, Month = 1, PaidAmount = 9999, State = PaymentStateEnum.Approved, PaymentDate = today });
        _context.DailyWorkRecords.AddRange(
            new DailyWorkRecord { UserId = userId, Date = today, State = WorkDayStateEnum.PaidLeave },
            new DailyWorkRecord { UserId = userId, Date = today.AddDays(1), State = WorkDayStateEnum.PaidLeave },
            new DailyWorkRecord { UserId = otherUser, Date = today, State = WorkDayStateEnum.PaidLeave });
        await _context.SaveChangesAsync();
        var service = new PaymentsService(new PaymentsRepository(_context), Mock.Of<IApplicationUsersRepository>(), new DailyWorkRecordsRepository(_context));
        LogArrange(nameof(userId), userId);

        // Act
        var result = await service.GetUserOverviewAsync(userId);
        LogAct(result);

        // Assert
        result.Value!.ApprovedAmount.Should().Be(100);
        result.Value.PendingAmount.Should().Be(200);
        result.Value.PaidLeaveDays.Should().Be(1);
        result.Value.LatestApproved!.PaidAmount.Should().Be(300);
        result.Value.LatestApproved.UserId.Should().Be(userId);
    }

    [Fact]
    public async Task AdminSummary_ShouldReturnOverallTotalsIndependentlyFromTheFilteredPage()
    {
        // Arrange
        var userId = Guid.NewGuid();
        _context.Users.Add(new ApplicationUser { Id = userId, FullName = "Employee", Position = PositionsEnum.Employee });
        _context.Payments.AddRange(
            new Payment { UserId = userId, Year = 2025, Month = 1, PaidAmount = 100, State = PaymentStateEnum.Approved },
            new Payment { UserId = userId, Year = 2026, Month = 1, PaidAmount = 200, State = PaymentStateEnum.Approved },
            new Payment { UserId = userId, Year = 2026, Month = 2, PaidAmount = 300, State = PaymentStateEnum.Pending },
            new Payment { UserId = userId, Year = 2026, Month = 3, PaidAmount = 400, State = PaymentStateEnum.Rejected });
        await _context.SaveChangesAsync();
        var service = new PaymentsService(new PaymentsRepository(_context), Mock.Of<IApplicationUsersRepository>(), Mock.Of<IDailyWorkRecordsRepository>());
        LogArrange(nameof(userId), userId);

        // Act
        var page = await service.GetPaymentsAsync(new PaymentQueryDTO { Year = 2026, State = PaymentStateEnum.Pending, Taken = 0, SectionSize = 5 });
        var summary = await service.GetAdminSummaryAsync();
        LogAct(summary);

        // Assert
        page.Value!.TotalCount.Should().Be(1);
        page.Value.Items.Should().ContainSingle().Which.State.Should().Be(PaymentStateEnum.Pending);
        summary.Value!.TotalCount.Should().Be(4);
        summary.Value.ApprovedCount.Should().Be(2);
        summary.Value.PendingCount.Should().Be(1);
        summary.Value.RejectedCount.Should().Be(1);
        summary.Value.ApprovedAmount.Should().Be(300);
    }

    #endregion
    // ---------- OrganizationTree ----------
    #region OrganizationTree
    [Fact]
    public void OrganizationTree_ShouldMapFullNamesForParentsAndChildrenWhilePreservingUsernames()
    {
        // Arrange
        LogArrange(
            "Scenario",
            nameof(OrganizationTree_ShouldMapFullNamesForParentsAndChildrenWhilePreservingUsernames)
        );
        var parent = new ApplicationUser
        {
            FullName = "Parent Full Name",
            UserName = "parent.login",
            Position = PositionsEnum.Manager
        };
        var child = new ApplicationUser
        {
            FullName = "Child Full Name",
            UserName = "child.login",
            Position = PositionsEnum.Employee
        };
        var node = new OrganizationHierarchy
        {
            UserId = parent.Id,
            User = parent,
            Children = [new OrganizationHierarchy
            {
                UserId = child.Id,
                User = child
            }

            ]
        };

        // Act
        var result = node.ToDTO();
        LogAct(result);

        // Assert
        result.FullName.Should().Be("Parent Full Name");
        result.UserName.Should().Be("parent.login");
        result.Children.Should().ContainSingle().Which.FullName.Should().Be("Child Full Name");
    }

    public void Dispose() => _context.Dispose();
    #endregion
}
