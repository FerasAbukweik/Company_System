using AutoFixture;
using FluentAssertions;
using HR_System.Core.Abstraction.RepositoryContracts;
using HR_System.Core.Domain.Entities;
using HR_System.Core.Domain.Identity;
using HR_System.Core.DTO.LazyLoading;
using HR_System.Core.Enums;
using HR_System.Infrastructure;
using HR_System.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using Moq;
using Xunit.Abstractions;

namespace TestProject1.RepositoriesTests;

public class TasksRepositoryTests : RepositoryTestBase, IDisposable
{
    private readonly ITasksRepository _tasksRepository;
    private readonly ApplicationDbContext _dbContext;

    public TasksRepositoryTests(ITestOutputHelper output) : base(output)
    {
        var dbOptions = new DbContextOptionsBuilder<ApplicationDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options;
        _dbContext = new ApplicationDbContext(dbOptions);
        // caching lives in CachedTasksRepository (a Scrutor decorator) and is tested there
        _tasksRepository = new TasksRepository(_dbContext);
    }

    private AppTask CreateTask(Guid? userId = null, Guid? managerId = null, TaskStatusEnum? status = null, DateTime? createdAt = null)
    {
        return Fixture.Build<AppTask>()
            .With(t => t.UserId, userId ?? Guid.NewGuid())
            .With(
                    t => t.ManagerId,
                    managerId ?? Guid.NewGuid()
                )
            .With(
                t => t.Status,
                status ?? TaskStatusEnum.Pending
            )
            .With(
            t => t.CreatedAt,
            createdAt ?? DateTime.UtcNow
        )
            .Without(t => t.User)
            .Without(t => t.Manager)
            .Without(t => t.Approvals)
            .Create();
    }

    private ApplicationUser CreateUser(string? userName = null)
    {
        return Fixture.Build<ApplicationUser>()
            .With(u => u.UserName, userName ?? $"user_{Guid.NewGuid():N}")
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

    #region Add
    [Fact]
    public void Add_ShouldTrackEntityAsAdded()
    {
        // Arrange
        LogArrange("Scenario", nameof(Add_ShouldTrackEntityAsAdded));

        var task = CreateTask();

        // Act
        _tasksRepository.Add(task);
        LogAct("Operation completed");

        // Assert
        _dbContext.Entry(task).State.Should().Be(EntityState.Added);
        _dbContext.Tasks.Local.Should().Contain(task);
    }

    [Fact]
    public void Add_ShouldNotPersistToDatabase_BeforeSaveChangesIsCalled()
    {
        // Arrange
        LogArrange("Scenario", nameof(Add_ShouldNotPersistToDatabase_BeforeSaveChangesIsCalled));

        var task = CreateTask();

        // Act
        _tasksRepository.Add(task);
        LogAct("Operation completed");

        // Assert
        _dbContext.Tasks.AsNoTracking().Any(t => t.Id == task.Id).Should().BeFalse();
    }

    #endregion
    #region LazyGetUserTasksAsync
    [Fact]
    public async Task LazyGetUserTasksAsync_ShouldReturnOnlyTasksForGivenUser()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var otherUserId = Guid.NewGuid();
        var userTask = CreateTask(userId: userId);
        var otherUserTask = CreateTask(userId: otherUserId);
        _dbContext.Tasks.AddRange(userTask, otherUserTask);
        await _dbContext.SaveChangesAsync();
        var lazyData = new LazyDTO
        {
            Taken = 0,
            SectionSize = 10
        };
        LogArrange(nameof(userId), userId);

        // Act
        var result = await _tasksRepository.FilterAsync(
            task => task.UserId == userId && task.Status != TaskStatusEnum.Completed,
            orderBy: task => task.CreatedAt,
            orderByDesc: true,
            skip: lazyData.Taken,
            take: lazyData.SectionSize
        );
        LogAct(result);

        // Assert
        result.Should().ContainSingle(t => t.Id == userTask.Id);
        result.Should().NotContain(t => t.Id == otherUserTask.Id);
    }

    [Fact]
    public async Task LazyGetUserTasksAsync_ShouldExcludeCompletedTasks()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var pendingTask = CreateTask(userId: userId, status: TaskStatusEnum.Pending);
        var completedTask = CreateTask(userId: userId, status: TaskStatusEnum.Completed);
        _dbContext.Tasks.AddRange(pendingTask, completedTask);
        await _dbContext.SaveChangesAsync();
        var lazyData = new LazyDTO
        {
            Taken = 0,
            SectionSize = 10
        };
        LogArrange(nameof(userId), userId);

        // Act
        var result = await _tasksRepository.FilterAsync(
            task => task.UserId == userId && task.Status != TaskStatusEnum.Completed,
            orderBy: task => task.CreatedAt,
            orderByDesc: true,
            skip: lazyData.Taken,
            take: lazyData.SectionSize
        );
        LogAct(result);

        // Assert
        result.Should().ContainSingle(t => t.Id == pendingTask.Id);
        result.Should().NotContain(t => t.Id == completedTask.Id);
    }

    [Fact]
    public async Task LazyGetUserTasksAsync_ShouldReturnTasksOrderedByCreatedAtDescending()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var oldest = CreateTask(userId: userId, createdAt: DateTime.UtcNow.AddDays(-2));
        var middle = CreateTask(userId: userId, createdAt: DateTime.UtcNow.AddDays(-1));
        var newest = CreateTask(userId: userId, createdAt: DateTime.UtcNow);
        _dbContext.Tasks.AddRange(oldest, middle, newest);
        await _dbContext.SaveChangesAsync();
        var lazyData = new LazyDTO
        {
            Taken = 0,
            SectionSize = 10
        };
        LogArrange(nameof(userId), userId);

        // Act
        var result = await _tasksRepository.FilterAsync(
            task => task.UserId == userId && task.Status != TaskStatusEnum.Completed,
            orderBy: task => task.CreatedAt,
            orderByDesc: true,
            skip: lazyData.Taken,
            take: lazyData.SectionSize
        );
        LogAct(result);

        // Assert
        result.Select(t => t.Id).Should().ContainInOrder(newest.Id, middle.Id, oldest.Id);
    }

    [Fact]
    public async Task LazyGetUserTasksAsync_ShouldRespectSkipAndTake()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var tasks = Enumerable.Range(0, 5).Select(i => CreateTask(userId: userId, createdAt: DateTime.UtcNow.AddMinutes(-i))).ToList();
        _dbContext.Tasks.AddRange(tasks);
        await _dbContext.SaveChangesAsync();
        var lazyData = new LazyDTO
        {
            Taken = 1,
            SectionSize = 2
        };
        LogArrange(nameof(userId), userId);

        // Act
        var result = await _tasksRepository.FilterAsync(
            task => task.UserId == userId && task.Status != TaskStatusEnum.Completed,
            orderBy: task => task.CreatedAt,
            orderByDesc: true,
            skip: lazyData.Taken,
            take: lazyData.SectionSize
        );
        LogAct(result);

        // Assert — sorted desc by CreatedAt: [0,1,2,3,4] -> skip 1, take 2 -> [1,2]

        // Assert
        result.Should().HaveCount(2);
        result.Select(t => t.Id).Should().ContainInOrder(tasks[1].Id, tasks[2].Id);
    }

    [Fact]
    public async Task LazyGetUserTasksAsync_ShouldReturnEmptyList_WhenUserHasNoMatchingTasks()
    {
        // Arrange
        var lazyData = new LazyDTO
        {
            Taken = 0,
            SectionSize = 10
        };
        LogArrange(nameof(lazyData), lazyData);

        // Act
        var result = await _tasksRepository.FilterAsync(
            task => task.UserId == Guid.NewGuid() && task.Status != TaskStatusEnum.Completed,
            orderBy: task => task.CreatedAt,
            orderByDesc: true,
            skip: lazyData.Taken,
            take: lazyData.SectionSize
        );
        LogAct(result);

        // Assert
        result.Should().BeEmpty();
    }

    #endregion
    #region UpdateStatusAsync
    [Fact]
    public async Task UpdateStatusAsync_ShouldUpdateStatus_WhenTaskExists()
    {
        // Arrange
        LogArrange("Scenario", nameof(UpdateStatusAsync_ShouldUpdateStatus_WhenTaskExists));

        var task = CreateTask(status: TaskStatusEnum.Pending);
        _dbContext.Tasks.Add(task);
        await _dbContext.SaveChangesAsync();
        _dbContext.ChangeTracker.Clear();

        // Act
        var result = await _tasksRepository.UpdateStatusAsync(task.Id, TaskStatusEnum.Completed);
        LogAct(result);

        // Assert
        result.Should().NotBeNull();
        result!.Status.Should().Be(TaskStatusEnum.Completed);
    }

    [Fact]
    public async Task UpdateStatusAsync_ShouldPersistUpdatedStatus_AfterSaveChanges()
    {
        // Arrange
        LogArrange("Scenario", nameof(UpdateStatusAsync_ShouldPersistUpdatedStatus_AfterSaveChanges));

        var task = CreateTask(status: TaskStatusEnum.Pending);
        _dbContext.Tasks.Add(task);
        await _dbContext.SaveChangesAsync();
        _dbContext.ChangeTracker.Clear();

        // Act
        await _tasksRepository.UpdateStatusAsync(task.Id, TaskStatusEnum.Completed);
        await _dbContext.SaveChangesAsync();
        _dbContext.ChangeTracker.Clear();
        var fromDb = await _dbContext.Tasks.SingleAsync(t => t.Id == task.Id);
        LogAct(fromDb);

        // Assert
        fromDb.Status.Should().Be(TaskStatusEnum.Completed);
    }

    [Fact]
    public async Task UpdateStatusAsync_ShouldReturnNull_WhenTaskDoesNotExist()
    {
        // Arrange
        LogArrange("Scenario", nameof(UpdateStatusAsync_ShouldReturnNull_WhenTaskDoesNotExist));

        _dbContext.ChangeTracker.Clear();

        // Act
        var result = await _tasksRepository.UpdateStatusAsync(Guid.NewGuid(), TaskStatusEnum.Completed);
        LogAct(result);

        // Assert
        result.Should().BeNull();
    }

    #endregion
    #region GetTaskAsync
    [Fact]
    public async Task GetTaskAsync_ShouldIncludeUserNavigationProperty()
    {
        // Arrange
        LogArrange("Scenario", nameof(GetTaskAsync_ShouldIncludeUserNavigationProperty));

        var user = CreateUser("task_owner");
        _dbContext.Users.Add(user);
        var task = CreateTask(userId: user.Id);
        _dbContext.Tasks.Add(task);
        await _dbContext.SaveChangesAsync();
        _dbContext.ChangeTracker.Clear();

        // Act
        var result = await _tasksRepository.GetByIdAsync(task.Id, [record => record.User!]);
        LogAct(result);

        // Assert
        result.Should().NotBeNull();
        result!.User.Should().NotBeNull();
        result.User!.UserName.Should().Be("task_owner");
    }

    [Fact]
    public async Task GetTaskAsync_ShouldReturnNull_WhenTaskDoesNotExist()
    {
        // Arrange
        LogArrange("Scenario", nameof(GetTaskAsync_ShouldReturnNull_WhenTaskDoesNotExist));

        // Act
        var result = await _tasksRepository.GetByIdAsync(Guid.NewGuid());
        LogAct(result);

        // Assert
        result.Should().BeNull();
    }

    #endregion
    #region RetainedHistory
    [Fact]
    public async Task GetByIdAsync_ShouldRetainCompletedTasks()
    {
        // Arrange
        LogArrange("Scenario", nameof(GetByIdAsync_ShouldRetainCompletedTasks));

        var task = CreateTask(status: TaskStatusEnum.Completed);
        _dbContext.Tasks.Add(task);
        await _dbContext.SaveChangesAsync();
        _dbContext.ChangeTracker.Clear();

        // Act
        var result = await _tasksRepository.GetByIdAsync(task.Id);
        LogAct(result);

        // Assert
        result.Should().NotBeNull();
        result!.Status.Should().Be(TaskStatusEnum.Completed);
    }

    [Fact]
    public void Contract_ShouldNotExposeDeletionOfTaskHistory()
    {
        // Arrange
        LogArrange("Scenario", nameof(Contract_ShouldNotExposeDeletionOfTaskHistory));

        LogAct("Operation completed");

        // Act

        // Assert
        typeof(ITasksRepository).GetMethods().Should().NotContain(method => method.Name.Contains("Remove"));
    }

    #endregion
    #region SaveChangesAsync
    [Fact]
    public async Task SaveChangesAsync_ShouldReturnTrue_WhenThereArePendingChanges()
    {
        // Arrange
        LogArrange("Scenario", nameof(SaveChangesAsync_ShouldReturnTrue_WhenThereArePendingChanges));

        _dbContext.Tasks.Add(CreateTask());

        // Act
        var result = await _tasksRepository.SaveChangesAsync();
        LogAct(result);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task SaveChangesAsync_ShouldReturnFalse_WhenThereAreNoPendingChanges()
    {
        // Arrange
        LogArrange("Scenario", nameof(SaveChangesAsync_ShouldReturnFalse_WhenThereAreNoPendingChanges));

        // Act
        var result = await _tasksRepository.SaveChangesAsync();
        LogAct(result);

        // Assert
        result.Should().BeFalse();
    }

    #endregion
    public void Dispose()
    {
        _dbContext.Dispose();
    }
}
