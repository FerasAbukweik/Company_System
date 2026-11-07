using HR_System.Core.Domain.Entities;
using HR_System.Core.Enums;
using HR_System.Infrastructure;
using HR_System.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace TestProject1.RepositoriesTests;

public class PaymentsRepositoryTests : RepositoryTestBase, IDisposable
{
    private readonly ApplicationDbContext _dbContext = new(new DbContextOptionsBuilder<ApplicationDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);

    public PaymentsRepositoryTests(ITestOutputHelper output) : base(output)
    {
    }

    private static Payment CreatePayment(Guid? userId = null, int month = 10) => new()
    {
        UserId = userId ?? Guid.NewGuid(),
        Year = 2026,
        Month = month,
        PaidAmount = 1250.75m,
        PaymentDate = new DateOnly(2026, 11, 5)
    };
    // ---------- Add ----------
    #region Add
    [Fact]
    public async Task Add_ShouldDeferSavingAndReadWithoutTracking()
    {
        // Arrange
        LogArrange("Scenario", nameof(Add_ShouldDeferSavingAndReadWithoutTracking));

        var repository = new PaymentsRepository(_dbContext);
        var payment = CreatePayment();

        // Act
        repository.Add(payment);
        LogAct("Operation completed");

        // Assert
        (await _dbContext.Payments.AsNoTracking().ToListAsync()).Should().BeEmpty();
        (await repository.SaveChangesAsync()).Should().BeTrue();
        _dbContext.ChangeTracker.Clear();
        (await repository.GetByIdAsync(payment.Id))!.PaidAmount.Should().Be(1250.75m);
        _dbContext.ChangeTracker.Entries().Should().BeEmpty();
    }

    #endregion
    // ---------- Filter ----------
    #region Filter
    [Fact]
    public async Task Filter_ShouldApplyEmployeeScopeSortingAndPaging()
    {
        // Arrange
        var repository = new PaymentsRepository(_dbContext);
        var userId = Guid.NewGuid();
        _dbContext.AddRange(
            CreatePayment(userId, 8),
            CreatePayment(userId, 9),
            CreatePayment(userId, 10),
            CreatePayment()
        );
        await _dbContext.SaveChangesAsync();
        _dbContext.ChangeTracker.Clear();
        LogArrange(nameof(userId), userId);

        // Act
        var payments = await repository.FilterAsync(p => p.UserId == userId, orderBy: p => p.Month, orderByDesc: true, skip: 1, take: 1);
        LogAct(payments);

        // Assert
        payments.Should().ContainSingle().Which.Month.Should().Be(9);
        _dbContext.ChangeTracker.Entries().Should().BeEmpty();
    }

    #endregion
    // ---------- UpdateState ----------
    #region UpdateState
    [Fact]
    public async Task UpdateState_ShouldStageStateAndApprovalDate()
    {
        // Arrange
        LogArrange("Scenario", nameof(UpdateState_ShouldStageStateAndApprovalDate));

        var repository = new PaymentsRepository(_dbContext);
        var payment = CreatePayment();
        _dbContext.Add(payment);
        await _dbContext.SaveChangesAsync();
        _dbContext.ChangeTracker.Clear();

        // Act
        var updated = await repository.UpdateStateAsync(payment.Id, PaymentStateEnum.Approved);
        LogAct(updated);

        // Assert
        _dbContext.Entry(updated!).Properties.Where(property => property.IsModified).Select(property => property.Metadata.Name).Should().BeEquivalentTo(["State", "PaymentDate"]);
        (await repository.GetByIdAsync(payment.Id))!.State.Should().Be(PaymentStateEnum.Pending);
        await repository.SaveChangesAsync();
        var saved = await repository.GetByIdAsync(payment.Id);
        saved!.State.Should().Be(PaymentStateEnum.Approved);
        saved.PaidAmount.Should().Be(payment.PaidAmount);
        saved.PaymentDate.Should().Be(DateOnly.FromDateTime(DateTime.UtcNow));
        saved.Month.Should().Be(payment.Month);
    }

    #endregion
    // ---------- Model ----------
    #region Model
    [Fact]
    public void Model_ShouldEnforceUniqueEmployeeMonthAndRetainPaymentHistory()
    {
        // Arrange
        LogArrange("Scenario", nameof(Model_ShouldEnforceUniqueEmployeeMonthAndRetainPaymentHistory));

        var entity = _dbContext.Model.FindEntityType(typeof(Payment))!;

        // Act
        var index = entity.GetIndexes().Should().ContainSingle().Which;
        LogAct(index);

        // Assert
        index.IsUnique.Should().BeTrue();
        index.Properties.Select(p => p.Name).Should().Equal(new[] { "UserId", "Year", "Month" });
        entity.GetForeignKeys().Should().ContainSingle().Which.DeleteBehavior.Should().Be(DeleteBehavior.Restrict);
    }

    #endregion
    // ---------- Migration ----------
    #region Migration
    [Fact]
    public void Migration_ShouldOnlyAddPaymentsAndMatchTheCurrentModel()
    {
        // Arrange
        LogArrange("Scenario", nameof(Migration_ShouldOnlyAddPaymentsAndMatchTheCurrentModel));

        // Act
        using var context = new ApplicationDbContext(new DbContextOptionsBuilder<ApplicationDbContext>().UseSqlServer("Server=localhost;Database=MigrationValidation;Trusted_Connection=True;TrustServerCertificate=True").Options);
        LogAct(context);

        // Assert
        context.Database.HasPendingModelChanges().Should().BeFalse();
        var script = context.GetService<IMigrator>().GenerateScript("20261105144635_AddDailyWorkRecords", "20261105195622_MakePaymentDateApprovalOnly");
        script.Should().Contain("CREATE TABLE [Payments]");
        script.Should().Contain("[PaidAmount] decimal(18,2) NOT NULL");
        script.Should().Contain("CREATE UNIQUE INDEX [IX_Payments_UserId_Year_Month]");
        script.Should().Contain("CHECK ([PaidAmount] > 0)");
        script.Should().Contain("CHECK ([State] IN (0, 1, 2))");
        script.Should().NotContain("DROP COLUMN");
        script.Should().NotContain("CREATE TABLE [Images]");
        script.Should().NotContain("CREATE TABLE [DailyWorkRecords]");
    }

    public void Dispose() => _dbContext.Dispose();
    #endregion
}
