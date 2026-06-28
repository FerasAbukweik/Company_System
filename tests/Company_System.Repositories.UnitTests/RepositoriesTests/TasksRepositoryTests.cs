using AutoFixture;
using FluentAssertions;
using HR_System.Core.Domain.Entities;
using HR_System.Core.Domain.Identity;
using HR_System.Core.DTO.LazyLoading;
using HR_System.Core.Enums;
using HR_System.Core.Interfaces.RepositoryContracts;
using HR_System.Infrastructure;
using HR_System.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using Xunit.Abstractions;

namespace TestProject1.RepositoriesTests;

public class TasksRepositoryTests : IDisposable
{
    private readonly ITasksRepository _tasksRepository;
    private readonly ApplicationDbContext _dbContext;
    private readonly ITestOutputHelper _output;
    private readonly IFixture _fixture;

    private readonly Guid _userId = Guid.NewGuid();
    private readonly Guid _managerId = Guid.NewGuid();

    public TasksRepositoryTests(ITestOutputHelper output)
    {
        _output = output;

        _fixture = new Fixture();
        _fixture.Behaviors.OfType<ThrowingRecursionBehavior>().ToList()
            .ForEach(b => _fixture.Behaviors.Remove(b));
        _fixture.Behaviors.Add(new OmitOnRecursionBehavior());

        var dbOptions = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        _dbContext = new ApplicationDbContext(dbOptions);
        _tasksRepository = new TasksRepository(_dbContext);
    }

    #region Add

    [Fact]
    public async Task Add_ValidTask_ShouldPersistAfterSave()
    {
        // Arrange
        var task = CreateTask();
        _output.WriteLine($"Adding Task: {task.Id} | {task.Title}");

        // Act
        _tasksRepository.Add(task);
        await _tasksRepository.SaveChangesAsync();

        // Assert
        var actual = await _tasksRepository.GetTaskAsync(task.Id);
        _output.WriteLine($"Actual Id   : {actual?.Id}");
        _output.WriteLine($"Actual Title: {actual?.Title}");

        actual.Should().NotBeNull();
        actual!.Id.Should().Be(task.Id);
    }

    [Fact]
    public async Task Add_MultipleTasks_ShouldPersistAll()
    {
        // Arrange
        var tasks = CreateMany(3, userId: _userId);
        _output.WriteLine($"Expected Count: {tasks.Count}");
        tasks.ForEach(t => _output.WriteLine($"  Task: {t.Id} | {t.Title}"));

        // Act
        foreach (var task in tasks)
            _tasksRepository.Add(task);
        await _tasksRepository.SaveChangesAsync();

        // Assert
        var actual = await _tasksRepository.LazyGetUserTasksAsync(_userId, new LazyDTO { Taken = 0, SectionSize = 10 });
        _output.WriteLine($"Actual Count: {actual.Count}");

        actual.Should().HaveCount(tasks.Count);
    }

    #endregion

    #region LazyGetUserTasksAsync

    [Fact]
    public async Task LazyGetUserTasksAsync_UserWithTasks_ShouldReturnOnlyUserTasks()
    {
        // Arrange
        var userTasks = CreateMany(3, userId: _userId);
        var otherTasks = CreateMany(3); // random userIds
        await SeedAsync([.. userTasks, .. otherTasks]);

        _output.WriteLine($"UserId        : {_userId}");
        _output.WriteLine($"Expected Count: {userTasks.Count}");
        userTasks.ForEach(t => _output.WriteLine($"  Expected: {t.Id} | UserId: {t.UserId}"));

        // Act
        var actual = await _tasksRepository.LazyGetUserTasksAsync(_userId, new LazyDTO { Taken = 0, SectionSize = 10 });
        _output.WriteLine($"Actual Count: {actual.Count}");
        actual.ToList().ForEach(t => _output.WriteLine($"  Actual: {t.Id} | UserId: {t.UserId}"));

        // Assert
        actual.Should().NotBeNull();
        actual.Should().HaveCount(userTasks.Count);
        actual.Should().OnlyContain(t => t.UserId == _userId);
    }

    [Fact]
    public async Task LazyGetUserTasksAsync_ShouldSkipCorrectly()
    {
        // Arrange
        var tasks = CreateMany(5, userId: _userId);
        await SeedAsync([.. tasks]);

        var lazyData = new LazyDTO { Taken = 2, SectionSize = 10 };
        _output.WriteLine($"Total Seeded: {tasks.Count}");
        _output.WriteLine($"Taken (skip): {lazyData.Taken}");
        _output.WriteLine($"Expected Count: {tasks.Count - lazyData.Taken}");

        // Act
        var actual = await _tasksRepository.LazyGetUserTasksAsync(_userId, lazyData);
        _output.WriteLine($"Actual Count: {actual.Count}");

        // Assert
        actual.Should().HaveCount(tasks.Count - lazyData.Taken);
    }

    [Fact]
    public async Task LazyGetUserTasksAsync_ShouldTakeCorrectly()
    {
        // Arrange
        var tasks = CreateMany(10, userId: _userId);
        await SeedAsync([.. tasks]);

        var lazyData = new LazyDTO { Taken = 0, SectionSize = 4 };
        _output.WriteLine($"Total Seeded: {tasks.Count}");
        _output.WriteLine($"SectionSize : {lazyData.SectionSize}");
        _output.WriteLine($"Expected Count: {lazyData.SectionSize}");

        // Act
        var actual = await _tasksRepository.LazyGetUserTasksAsync(_userId, lazyData);
        _output.WriteLine($"Actual Count: {actual.Count}");

        // Assert
        actual.Should().HaveCount(lazyData.SectionSize);
    }

    [Fact]
    public async Task LazyGetUserTasksAsync_SkipAndTake_ShouldReturnCorrectPage()
    {
        // Arrange
        var tasks = CreateMany(10, userId: _userId);
        await SeedAsync([.. tasks]);

        var lazyData = new LazyDTO { Taken = 3, SectionSize = 4 };
        _output.WriteLine($"Total Seeded : {tasks.Count}");
        _output.WriteLine($"Taken (skip) : {lazyData.Taken}");
        _output.WriteLine($"SectionSize  : {lazyData.SectionSize}");
        _output.WriteLine($"Expected Count: {lazyData.SectionSize}");

        // Act
        var actual = await _tasksRepository.LazyGetUserTasksAsync(_userId, lazyData);
        _output.WriteLine($"Actual Count: {actual.Count}");

        // Assert
        actual.Should().HaveCount(lazyData.SectionSize);
        actual.Should().OnlyContain(t => t.UserId == _userId);
    }

    [Fact]
    public async Task LazyGetUserTasksAsync_UserWithNoTasks_ShouldReturnEmpty()
    {
        // Arrange
        var otherTasks = CreateMany(3);
        await SeedAsync([.. otherTasks]);

        _output.WriteLine($"UserId        : {_userId}");
        _output.WriteLine("Expected Count: 0");

        // Act
        var actual = await _tasksRepository.LazyGetUserTasksAsync(_userId, new LazyDTO { Taken = 0, SectionSize = 10 });
        _output.WriteLine($"Actual Count: {actual.Count}");

        // Assert
        actual.Should().NotBeNull();
        actual.Should().BeEmpty();
    }

    [Fact]
    public async Task LazyGetUserTasksAsync_SkipMoreThanExists_ShouldReturnEmpty()
    {
        // Arrange
        var tasks = CreateMany(3, userId: _userId);
        await SeedAsync([.. tasks]);

        var lazyData = new LazyDTO { Taken = 10, SectionSize = 5 };
        _output.WriteLine($"Total Seeded: {tasks.Count}");
        _output.WriteLine($"Taken (skip): {lazyData.Taken}");
        _output.WriteLine("Expected Count: 0");

        // Act
        var actual = await _tasksRepository.LazyGetUserTasksAsync(_userId, lazyData);
        _output.WriteLine($"Actual Count: {actual.Count}");

        // Assert
        actual.Should().BeEmpty();
    }

    [Fact]
    public async Task LazyGetUserTasksAsync_ShouldReturnReadOnlyList()
    {
        // Arrange
        var task = CreateTask(userId: _userId);
        await SeedAsync(task);

        // Act
        var actual = await _tasksRepository.LazyGetUserTasksAsync(_userId, new LazyDTO { Taken = 0, SectionSize = 10 });
        _output.WriteLine($"Actual Type: {actual.GetType().Name}");

        // Assert
        actual.Should().BeAssignableTo<IReadOnlyList<AppTask>>();
    }

    #endregion

    #region GetTaskAsync

    [Fact]
    public async Task GetTaskAsync_ExistingTask_ShouldReturnTask()
    {
        // Arrange
        var task = CreateTask();
        await SeedAsync(task);

        _output.WriteLine($"Expected Id   : {task.Id}");
        _output.WriteLine($"Expected Title: {task.Title}");

        // Act
        var actual = await _tasksRepository.GetTaskAsync(task.Id);
        _output.WriteLine($"Actual Id   : {actual?.Id}");
        _output.WriteLine($"Actual Title: {actual?.Title}");

        // Assert
        actual.Should().NotBeNull();
        actual!.Id.Should().Be(task.Id);
    }

    [Fact]
    public async Task GetTaskAsync_NonExistentTask_ShouldReturnNull()
    {
        // Arrange
        var nonExistentId = Guid.NewGuid();
        _output.WriteLine($"Non-existent Task Id: {nonExistentId}");

        // Act
        var actual = await _tasksRepository.GetTaskAsync(nonExistentId);
        _output.WriteLine($"Actual: {actual?.ToString() ?? "null"}");

        // Assert
        actual.Should().BeNull();
    }

    #endregion

    #region UpdateStatusAsync

    [Fact]
    public async Task UpdateStatusAsync_ValidTask_ShouldReturnTaskWithNewStatus()
    {
        // Arrange
        var task = CreateTask(status: TaskStatusEnum.Pending);
        await SeedAsync(task);
        var newStatus = TaskStatusEnum.Pending;

        _output.WriteLine($"Task Id        : {task.Id}");
        _output.WriteLine($"Initial Status : {task.Status}");
        _output.WriteLine($"Expected Status: {newStatus}");

        // Act
        var actual = await _tasksRepository.UpdateStatusAsync(task.Id, newStatus);
        _output.WriteLine($"Actual Status: {actual?.Status}");

        // Assert
        actual.Should().NotBeNull();
        actual!.Status.Should().Be(newStatus);
    }

    [Fact]
    public async Task UpdateStatusAsync_ValidTask_ShouldPersistAfterSave()
    {
        // Arrange
        var task = CreateTask(status: TaskStatusEnum.Pending);
        await SeedAsync(task);
        var newStatus = TaskStatusEnum.Pending;

        _output.WriteLine($"Task Id        : {task.Id}");
        _output.WriteLine($"Expected Status: {newStatus}");

        // Act
        await _tasksRepository.UpdateStatusAsync(task.Id, newStatus);
        await _tasksRepository.SaveChangesAsync();

        // Assert — re-fetch via GetTaskAsync
        var actual = await _tasksRepository.GetTaskAsync(task.Id);
        _output.WriteLine($"Actual Status (persisted): {actual?.Status}");

        actual.Should().NotBeNull();
        actual!.Status.Should().Be(newStatus);
    }

    [Fact]
    public async Task UpdateStatusAsync_NonExistentTask_ShouldReturnNull()
    {
        // Arrange
        var nonExistentId = Guid.NewGuid();
        _output.WriteLine($"Non-existent Task Id: {nonExistentId}");

        // Act
        var actual = await _tasksRepository.UpdateStatusAsync(nonExistentId, TaskStatusEnum.Pending);
        _output.WriteLine($"Actual: {actual?.ToString() ?? "null"}");

        // Assert
        actual.Should().BeNull();
    }

    [Fact]
    public async Task UpdateStatusAsync_ShouldNotAffectOtherTasks()
    {
        // Arrange
        var task1 = CreateTask(userId: _userId, status: TaskStatusEnum.Pending);
        var task2 = CreateTask(userId: _userId, status: TaskStatusEnum.Pending);
        await SeedAsync(task1, task2);

        _output.WriteLine($"Updating Task1: {task1.Id} → Pending");
        _output.WriteLine($"Task2 should stay Pending: {task2.Id}");

        // Act
        await _tasksRepository.UpdateStatusAsync(task1.Id, TaskStatusEnum.Pending);
        await _tasksRepository.SaveChangesAsync();

        // Assert
        var actual = await _tasksRepository.GetTaskAsync(task2.Id);
        _output.WriteLine($"Task2 Expected: {TaskStatusEnum.Pending}");
        _output.WriteLine($"Task2 Actual  : {actual?.Status}");

        actual!.Status.Should().Be(TaskStatusEnum.Pending);
    }

    #endregion

    #region SaveChangesAsync

    [Fact]
    public async Task SaveChangesAsync_WithPendingChanges_ShouldReturnTrue()
    {
        // Arrange
        var task = CreateTask();
        _tasksRepository.Add(task);
        _output.WriteLine($"Added Task: {task.Id}");

        // Act
        var actual = await _tasksRepository.SaveChangesAsync();
        _output.WriteLine($"Expected: true | Actual: {actual}");

        // Assert
        actual.Should().BeTrue();
    }

    [Fact]
    public async Task SaveChangesAsync_WithNoChanges_ShouldReturnFalse()
    {
        // Arrange
        _output.WriteLine("No changes made");

        // Act
        var actual = await _tasksRepository.SaveChangesAsync();
        _output.WriteLine($"Expected: false | Actual: {actual}");

        // Assert
        actual.Should().BeFalse();
    }

    #endregion

    #region Helpers

    private AppTask CreateTask(Guid? userId = null, TaskStatusEnum? status = null) =>
        _fixture.Build<AppTask>()
            .With(t => t.UserId, userId ?? Guid.NewGuid())
            .With(t => t.User, null as ApplicationUser)
            .With(t => t.ManagerId, _managerId)
            .With(t => t.Manager, null as ApplicationUser)
            .With(t => t.Status, status ?? _fixture.Create<TaskStatusEnum>())
            .Create();

    private List<AppTask> CreateMany(int count, Guid? userId = null, TaskStatusEnum? status = null) =>
        Enumerable.Range(0, count)
            .Select(_ => CreateTask(userId, status))
            .ToList();

    private async Task SeedAsync(params AppTask[] tasks)
    {
        await _dbContext.Tasks.AddRangeAsync(tasks);
        await _dbContext.SaveChangesAsync();
    }

    public void Dispose() => _dbContext.Dispose();

    #endregion
}