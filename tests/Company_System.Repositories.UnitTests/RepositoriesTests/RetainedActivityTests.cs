using HR_System.Core.Domain.Entities;
using HR_System.Core.Domain.Identity;
using HR_System.Core.Enums;
using HR_System.Infrastructure;
using HR_System.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using Moq;

namespace TestProject1.RepositoriesTests;

public class RetainedActivityTests : RepositoryTestBase
{
    public RetainedActivityTests(ITestOutputHelper output) : base(output)
    {
    }

    // ---------- CompletedTask ----------
    #region CompletedTask
    [Fact]
    public async Task CompletedTask_ShouldRemainAvailableToApprovalsAndActivityWithoutCopiedText()
    {
        // Arrange
        LogArrange("Scenario", nameof(CompletedTask_ShouldRemainAvailableToApprovalsAndActivityWithoutCopiedText));

        await using var db = new ApplicationDbContext(new DbContextOptionsBuilder<ApplicationDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);
        var manager = new ApplicationUser
        {
            FullName = "Manager",
            UserName = "manager",
            Position = PositionsEnum.Employee
        };
        var employee = new ApplicationUser
        {
            FullName = "Employee",
            UserName = "employee",
            Position = PositionsEnum.Employee
        };
        var task = new AppTask
        {
            Title = "Original title",
            Description = "Task description",
            CreatedAt = DateTime.UtcNow,
            Deadline = DateTime.UtcNow.AddDays(1),
            Priority = default,
            UserId = employee.Id,
            ManagerId = manager.Id
        };
        var approval = new Approval
        {
            Type = ApprovalTypeEnum.Task,
            TaskId = task.Id,
            ManagerId = manager.Id,
            UserRequestingId = employee.Id
        };
        var activity = new Activity
        {
            Type = ActivityTypeEnum.ApprovalPending,
            ApprovalId = approval.Id,
            TriggeredById = employee.Id
        };
        db.Users.AddRange(manager, employee);
        db.Tasks.Add(task);
        db.Approvals.Add(approval);
        db.Activities.Add(activity);
        await db.SaveChangesAsync();
        var repository = new TasksRepository(db);
        db.ChangeTracker.Clear();
        // Act
        var completed = await repository.UpdateStatusAsync(task.Id, TaskStatusEnum.Completed);
        var savedApproval = await db.Approvals.SingleAsync(record => record.Id == approval.Id);
        savedApproval.Status = ApprovalStatusEnum.Approved;
        completed!.Title = "Current title";
        await db.SaveChangesAsync();

        db.ChangeTracker.Clear();
        LogAct(completed);

        // Assert
        (await repository.FilterAsync(record => record.UserId == employee.Id && record.Status != TaskStatusEnum.Completed)).Should().BeEmpty();
        (await repository.GetByIdAsync(task.Id))!.Title.Should().Be("Current title");
        var historical = await new ActivityRepository(db).GetByIdAsync(activity.Id, [record => record.Approval!.Task]);
        historical!.Title.Should().BeNull();
        historical.Description.Should().BeNull();
        var dto = historical.ToDTO();
        dto.Description.Should().Contain("Current title");
        dto.Description.Should().Contain("Approval Status: Pending");
        dto.Description.Should().NotContain("Approval Status: Approved");
        var retainedApproval = await db.Approvals.Include(a => a.Task).SingleAsync(a => a.Id == approval.Id);
        retainedApproval.ToRequestedApprovalDTO().Body.Should().Contain("Current title");
    }

    #endregion
    // ---------- LegacyActivity ----------
    #region LegacyActivity
    [Fact]
    public void LegacyActivity_ShouldKeepExistingText()
    {
        // Arrange
        LogArrange("Scenario", nameof(LegacyActivity_ShouldKeepExistingText));

        // Act
        var activity = new Activity
        {
            Type = ActivityTypeEnum.TaskAdded,
            TriggeredById = Guid.NewGuid(),
            Title = "Existing title",
            Description = "Existing description"
        };
        LogAct(activity);

        // Assert
        activity.ToDTO().Title.Should().Be("Existing title");
        activity.ToDTO().Description.Should().Be("Existing description");
    }
    #endregion
}
