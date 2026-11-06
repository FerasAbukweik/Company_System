using HR_System.Core.Abstraction.ServiceContracts;
using HR_System.Core.Abstraction.RepositoryContracts;
using HR_System.Core.Domain.Identity;
using HR_System.Core.Domain.Entities;
using HR_System.Core.DTO.Users;
using HR_System.Core.Enums;
using HR_System.Infrastructure;
using HR_System.Infrastructure.Repositories;
using HR_System.Infrastructure.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Moq;

namespace HR_System.Core.UnitTests.ServicesTests;

public class EmployeeAdminListingTests : ServiceTestBase, IDisposable
{
    private readonly ApplicationDbContext _context = new(new DbContextOptionsBuilder<ApplicationDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);

    public EmployeeAdminListingTests(ITestOutputHelper output) : base(output)
    {
    }

    private UsersService Service() => new(null !, new ApplicationUsersRepository(_context, new UpperInvariantLookupNormalizer()), Mock.Of<IOrganizationHierarchyService>(), Mock.Of<IImageService>(), Mock.Of<IOrganizationHierarchyRepository>());
    private async Task Seed()
    {
        for (var i = 0; i < 13; i++)
        {
            var employee = new ApplicationUser
            {
                FullName = $"Employee {i}",
                UserName = $"employee{i}",
                Email = $"employee{i}@example.com",
                Position = PositionsEnum.Employee,
                HourlyRate = 12000
            };
            employee.OrganizationHierarchy = new OrganizationHierarchy
            {
                UserId = employee.Id,
                IsActive = i % 2 == 0
            };
            _context.Add(employee);
        }

        await _context.SaveChangesAsync();
        _context.ChangeTracker.Clear();
    }

    // ---------- Listing ----------
    #region Listing
    [Fact]
    public async Task Listing_ShouldOrderNewestEmployeesBeforePagingAndKeepLegacyDatesLast()
    {
        // Arrange
        LogArrange("Scenario", nameof(Listing_ShouldOrderNewestEmployeesBeforePagingAndKeepLegacyDatesLast));

        var legacy = new ApplicationUser
        {
            FullName = "Legacy",
            Position = PositionsEnum.Employee,
            CreatedAt = null
        };
        var older = new ApplicationUser
        {
            FullName = "Older",
            Position = PositionsEnum.Employee,
            CreatedAt = new DateTime(2026, 1, 1)
        };
        var newest = new ApplicationUser
        {
            FullName = "Newest",
            Position = PositionsEnum.Employee,
            CreatedAt = new DateTime(2026, 11, 1)
        };
        _context.AddRange(legacy, newest, older);
        await _context.SaveChangesAsync();

        // Act
        var first = await Service().LazyGetEmployeesAsync(new EmployeeQueryDTO { Taken = 0, SectionSize = 1 });
        var rest = await Service().LazyGetEmployeesAsync(new EmployeeQueryDTO { Taken = 1, SectionSize = 5 });
        LogAct(first);

        // Assert
        first.Value!.Items.Should().ContainSingle().Which.Id.Should().Be(newest.Id);
        rest.Value!.Items.Select(employee => employee.Id).Should().Equal(new[] { older.Id, legacy.Id });
    }

    [Fact]
    public async Task Listing_ShouldReturnOnlyTheRequestedPageWithFullMatchingTotals()
    {
        // Arrange
        LogArrange("Scenario", nameof(Listing_ShouldReturnOnlyTheRequestedPageWithFullMatchingTotals));

        await Seed();

        // Act
        var first = await Service().LazyGetEmployeesAsync(new EmployeeQueryDTO { Taken = 0, SectionSize = 5 });
        var next = await Service().LazyGetEmployeesAsync(new EmployeeQueryDTO { Taken = 5, SectionSize = 5 });
        var last = await Service().LazyGetEmployeesAsync(new EmployeeQueryDTO { Taken = 10, SectionSize = 5 });
        LogAct(first);

        // Assert
        first.Value!.Items.Count.Should().Be(5);
        next.Value!.Items.Count.Should().Be(5);
        last.Value!.Items.Count.Should().Be(3);
        first.Value.Items.Select(employee => employee.Id).Intersect(next.Value.Items.Select(employee => employee.Id)).Should().BeEmpty();
        first.Value.TotalCount.Should().Be(13);
        first.Value.Summary.Active.Should().Be(7);
        first.Value.Summary.TotalHourlyRates.Should().Be(156000);
        first.Value.Summary.ConfiguredHourlyRates.Should().Be(13);
        _context.ChangeTracker.Entries().Should().BeEmpty();
    }

    [Fact]
    public async Task Listing_ShouldFilterBeforePagingAndCounting()
    {
        // Arrange
        LogArrange("Scenario", nameof(Listing_ShouldFilterBeforePagingAndCounting));

        await Seed();

        // Act
        var result = await Service().LazyGetEmployeesAsync(new EmployeeQueryDTO { Taken = 0, SectionSize = 1, Search = "EMPLOYEE 1" });
        LogAct(result);

        // Assert
        result.Value!.Items.Should().ContainSingle();
        result.Value.TotalCount.Should().Be(4);
        result.Value.Summary.TotalHourlyRates.Should().Be(48000);
    }

    [Theory]
    [InlineData(-1, 5)]
    [InlineData(0, 0)]
    [InlineData(0, -1)]
    public async Task Listing_ShouldRejectInvalidPaging(int taken, int size)
    {
        // Arrange
        LogArrange(nameof(taken), taken);

        // Act
        var result = await Service().LazyGetEmployeesAsync(new EmployeeQueryDTO { Taken = taken, SectionSize = size });
        LogAct(result);

        // Assert
        result.IsSuccess.Should().BeFalse();
    }

    [Fact]
    public async Task Listing_ShouldNotLimitPositivePageSizesAndShouldHandleNoMatches()
    {
        // Arrange
        LogArrange("Scenario", nameof(Listing_ShouldNotLimitPositivePageSizesAndShouldHandleNoMatches));

        await Seed();

        // Act
        var all = await Service().LazyGetEmployeesAsync(new EmployeeQueryDTO { Taken = 0, SectionSize = 1000 });
        LogAct(all);

        // Assert
        all.Value!.Items.Count.Should().Be(13);
        var wrongPosition = await Service().LazyGetEmployeesAsync(new EmployeeQueryDTO { Taken = 0, SectionSize = 5, Search = "no-match" });
        wrongPosition.Value!.Items.Should().BeEmpty();
        wrongPosition.Value.TotalCount.Should().Be(0);
    }

    #endregion
    // ---------- Search ----------
    #region Search
    [Theory]
    [InlineData("sterling")]
    [InlineData("ALPHA.ACC")]
    [InlineData("@company.test")]
    [InlineData("791234")]
    [InlineData("program")]
    [InlineData("active")]
    public async Task Search_ShouldMatchEmployeeFieldsUsingOneCaseInsensitiveInput(string search)
    {
        // Arrange
        var user = new ApplicationUser
        {
            FullName = "Alex Sterling",
            UserName = "alpha.account",
            Email = "alex@company.test",
            PhoneNumber = "+962791234567",
            Position = PositionsEnum.Programmer
        };
        user.OrganizationHierarchy = new OrganizationHierarchy
        {
            UserId = user.Id,
            IsActive = true
        };
        _context.Add(user);
        await _context.SaveChangesAsync();
        _context.ChangeTracker.Clear();
        LogArrange(nameof(search), search);

        // Act
        var result = await Service().LazyGetEmployeesAsync(new EmployeeQueryDTO { Taken = 0, SectionSize = 5, Search = search });
        LogAct(result);

        // Assert
        result.Value!.Items.Should().ContainSingle().Which.Id.Should().Be(user.Id);
        result.Value.TotalCount.Should().Be(1);
    }

    [Fact]
    public async Task Search_ShouldSupportEmployeeIdsAndDistinguishActiveFromInactive()
    {
        // Arrange
        await Seed();
        var userId = await _context.Users.Select(user => user.Id).FirstAsync();
        var byId = await Service().LazyGetEmployeesAsync(new EmployeeQueryDTO { Taken = 0, SectionSize = 5, Search = userId.ToString() });
        LogArrange(nameof(userId), userId);

        // Act
        var inactive = await Service().LazyGetEmployeesAsync(new EmployeeQueryDTO { Taken = 0, SectionSize = 20, Search = "inactive" });
        LogAct(inactive);

        // Assert
        byId.Value!.Items.Should().ContainSingle().Which.Id.Should().Be(userId);
        inactive.Value!.TotalCount.Should().Be(6);
        inactive.Value.Items.Should().AllSatisfy(employee => employee.IsActive.Should().BeFalse());
    }

    public void Dispose() => _context.Dispose();
    #endregion
    [Fact]
    public async Task GetUser_ShouldReturnCurrentDatabaseProfileAndPayrollConfiguration()
    {
        // Arrange
        var user = new ApplicationUser
        {
            FullName = "Current Full Name", UserName = "current-user", Email = "current@example.com",
            PhoneNumber = "12345678", Position = PositionsEnum.Manager,
            HourlyRate = 27.50m, AnnualHolidayAllowance = 24
        };
        user.OrganizationHierarchy = new OrganizationHierarchy { UserId = user.Id, IsActive = true };
        _context.Users.Add(user);
        await _context.SaveChangesAsync();
        _context.ChangeTracker.Clear();
        LogArrange("UserId", user.Id);

        // Act
        var result = await Service().GetUserAsync(user.Id);
        LogAct(result);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value!.UserId.Should().Be(user.Id.ToString());
        result.Value.FullName.Should().Be("Current Full Name");
        result.Value.Email.Should().Be("current@example.com");
        result.Value.Phone.Should().Be("12345678");
        result.Value.Position.Should().Be(nameof(PositionsEnum.Manager));
        result.Value.HourlyRate.Should().Be(27.50m);
        result.Value.AnnualHolidayAllowance.Should().Be(24);
        result.Value.IsActive.Should().BeTrue();
        _context.ChangeTracker.Entries().Should().BeEmpty();
    }
}
