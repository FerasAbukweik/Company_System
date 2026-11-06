using HR_System.Core.Domain.Entities;
using HR_System.Core.Enums;
using HR_System.Infrastructure;
using HR_System.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace TestProject1.RepositoriesTests;

public class DailyWorkRecordsRepositoryTests : RepositoryTestBase, IDisposable
{
    private readonly ApplicationDbContext _dbContext = new(new DbContextOptionsBuilder<ApplicationDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);

    public DailyWorkRecordsRepositoryTests(ITestOutputHelper output) : base(output)
    {
    }

    private DailyWorkRecord CreateRecord(Guid? userId = null, int day = 5)
    {
        return new DailyWorkRecord()
        {
            UserId = userId ?? Guid.NewGuid(),
            Date = new DateOnly(2026, 11, day),
            MinutesWorked = 450,
            State = WorkDayStateEnum.Worked
        };
    }

    // ---------- Add ----------
    #region Add
    [Fact]
    public async Task Add_ShouldNotPersistUntilSaveChanges()
    {
        // Arrange
        LogArrange("Scenario", nameof(Add_ShouldNotPersistUntilSaveChanges));

        var repository = new DailyWorkRecordsRepository(_dbContext);
        var record = CreateRecord();

        // Act
        repository.Add(record);
        LogAct("Operation completed");

        // Assert
        (await _dbContext.DailyWorkRecords.AsNoTracking().ToListAsync()).Should().BeEmpty();
        (await repository.SaveChangesAsync()).Should().BeTrue();
        _dbContext.ChangeTracker.Clear();
        var saved = await repository.GetByIdAsync(record.Id);
        saved!.MinutesWorked.Should().Be(450);
        _dbContext.ChangeTracker.Entries().Should().BeEmpty();
    }

    #endregion
    // ---------- Filter ----------
    #region Filter
    [Fact]
    public async Task Filter_ShouldScopeByEmployeeAndDateAndApplyPaging()
    {
        // Arrange
        var repository = new DailyWorkRecordsRepository(_dbContext);
        var userId = Guid.NewGuid();
        _dbContext.AddRange(
            CreateRecord(userId, 3),
            CreateRecord(userId, 4),
            CreateRecord(userId, 5),
            CreateRecord(day: 4)
        );
        await _dbContext.SaveChangesAsync();
        _dbContext.ChangeTracker.Clear();
        LogArrange(nameof(userId), userId);

        // Act
        var records = await repository.FilterAsync(
            r => r.UserId == userId && r.Date >= new DateOnly(2026, 11, 4),
            orderBy: r => r.Date,
            skip: 1,
            take: 1
        );
        LogAct(records);

        // Assert
        records.Should().ContainSingle().Which.Date.Should().Be(new DateOnly(2026, 11, 5));
        _dbContext.ChangeTracker.Entries().Should().BeEmpty();
    }

    #endregion
    // ---------- Update ----------
    #region Update
    [Fact]
    public async Task Update_ShouldOnlyStageMinutesAndStateChanges()
    {
        // Arrange
        LogArrange("Scenario", nameof(Update_ShouldOnlyStageMinutesAndStateChanges));

        var repository = new DailyWorkRecordsRepository(_dbContext);
        var record = CreateRecord();
        _dbContext.Add(record);
        await _dbContext.SaveChangesAsync();
        _dbContext.ChangeTracker.Clear();

        // Act
        var updated = await repository.UpdateAsync(record.Id, 0, WorkDayStateEnum.PaidLeave);
        LogAct(updated);

        // Assert
        updated.Should().NotBeNull();
        var modified = _dbContext.Entry(updated).Properties.Where(p => p.IsModified).Select(p => p.Metadata.Name).ToArray();
        modified.Order().ToArray().Should().Equal(new[] { "MinutesWorked", "State" });
        (await repository.GetByIdAsync(record.Id))!.MinutesWorked.Should().Be(450);
        await repository.SaveChangesAsync();
        (await repository.GetByIdAsync(record.Id))!.State.Should().Be(WorkDayStateEnum.PaidLeave);
    }

    #endregion
    // ---------- Model ----------
    #region Model
    [Fact]
    public void Model_ShouldEnforceOneRecordPerEmployeeAndDateAndRetainHistory()
    {
        // Arrange
        LogArrange("Scenario", nameof(Model_ShouldEnforceOneRecordPerEmployeeAndDateAndRetainHistory));

        var entity = _dbContext.Model.FindEntityType(typeof(DailyWorkRecord))!;

        // Act
        var index = entity.GetIndexes().Should().ContainSingle().Which;
        LogAct(index);

        // Assert
        index.IsUnique.Should().BeTrue();
        index.Properties.Select(p => p.Name).Should().Equal(new[] { "UserId", "Date" });
        entity.GetForeignKeys().Should().ContainSingle().Which.DeleteBehavior.Should().Be(DeleteBehavior.Restrict);
        entity.FindProperty(nameof(DailyWorkRecord.MinutesWorked))!.ClrType.Should().Be(typeof(int));
    }

    #endregion
    // ---------- Migration ----------
    #region Migration
    [Fact]
    public void Migration_ShouldOnlyAddAttendanceSchemaAndMatchTheCurrentModel()
    {
        // Arrange
        LogArrange("Scenario", nameof(Migration_ShouldOnlyAddAttendanceSchemaAndMatchTheCurrentModel));

        // Act
        using var context = new ApplicationDbContext(new DbContextOptionsBuilder<ApplicationDbContext>().UseSqlServer("Server=localhost;Database=MigrationValidation;Trusted_Connection=True;TrustServerCertificate=True").Options);
        LogAct(context);

        // Assert
        context.Database.HasPendingModelChanges().Should().BeFalse();
        var script = context.GetService<IMigrator>().GenerateScript("20261104090000_AddImageEntity");
        script.Should().Contain("CREATE TABLE [DailyWorkRecords]");
        script.Should().Contain("CREATE UNIQUE INDEX [IX_DailyWorkRecords_UserId_Date]");
        script.Should().Contain("CHECK ([MinutesWorked] >= 0 AND [MinutesWorked] <= 1440)");
        script.Should().NotContain("DROP COLUMN");
        script.Should().NotContain("CREATE TABLE [Images]");
    }

    public void Dispose()
    {
        _dbContext.Dispose();
    }
    #endregion
}
