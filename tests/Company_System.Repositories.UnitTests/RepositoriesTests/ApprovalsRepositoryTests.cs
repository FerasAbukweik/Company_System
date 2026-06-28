using AutoFixture;
using FluentAssertions;
using Company_System.Infrastructure;
using HR_System.Core.Domain.Entities;
using HR_System.Core.Enums;
using HR_System.Infrastructure;
using HR_System.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using Xunit.Abstractions;

namespace TestProject1.RepositoriesTests;

public class ApprovalsRepositoryTests : IDisposable
{
    private readonly ApplicationDbContext _dbContext;
    private readonly TasksesRepository _repository;
    private readonly ITestOutputHelper _output;
    private readonly IFixture _fixture;

    private readonly Guid _userId = Guid.NewGuid();
    private readonly Guid _managerId = Guid.NewGuid();

    public ApprovalsRepositoryTests(ITestOutputHelper output)
    {
        _output = output;

        _fixture = new Fixture();
        _fixture.Behaviors
            .OfType<ThrowingRecursionBehavior>()
            .ToList()
            .ForEach(b => _fixture.Behaviors.Remove(b));
        _fixture.Behaviors.Add(new OmitOnRecursionBehavior());

        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        _dbContext = new ApplicationDbContext(options);
        _repository = new TasksesRepository(_dbContext);
    }

    #region Add

    [Fact]
    public async Task Add_ValidTask_ShouldPersistToDatabase()
    {
        // Arrange
        var task = CreateTask();
        _output.WriteLine($"Input Task Id   : {task.Id}");
        _output.WriteLine($"Input Task Title: {task.Title}");

        // Act
        _repository.Add(task);
        await _repository.SaveChangesAsync();

        // Assert — GetUserTasksAsync is the only read method available
        var actual = await _repository.GetUserTasksAsync(task.UserId);
        _output.WriteLine($"Actual Count: {actual.Count}");
        _output.WriteLine($"Actual Task Id: {actual.FirstOrDefault()?.Id}");

        actual.Should().ContainSingle(t => t.Id == task.Id);
    }

    [Fact]
    public async Task Add_MultipleTasks_ShouldPersistAll()
    {
        // Arrange
        var tasks = CreateMany(3);
        _output.WriteLine($"Expected Count: {tasks.Count}");
        tasks.ForEach(t => _output.WriteLine($"  Task: {t.Id} | {t.Title}"));

        // Act
        foreach (var task in tasks)
            _repository.Add(task);
        await _repository.SaveChangesAsync();

        // Assert
        var actual = await _repository.GetUserTasksAsync(_userId);
        _output.WriteLine($"Actual Count: {actual.Count}");

        actual.Should().HaveCount(tasks.Count);
    }

    [Fact]
    public async Task Add_WithoutSaving_ShouldNotPersist()
    {
        // Arrange
        var task = CreateTask();
        _output.WriteLine($"Task Id: {task.Id} — added but not saved");

        // Act — intentionally skip SaveChangesAsync
        _repository.Add(task);

        // Assert
        var actual = await _repository.GetUserTasksAsync(_userId);
        _output.WriteLine($"Actual Count: {actual.Count}");

        actual.Should().BeEmpty();
    }

    #endregion

    #region GetUserTasksAsync

    [Fact]
    public async Task GetUserTasksAsync_UserWithTasks_ShouldReturnOnlyUserTasks()
    {
        // Arrange
        var otherUserId = Guid.NewGuid();
        var userTasks = CreateMany(2, userId: _userId);
        var otherTask = CreateTask(userId: otherUserId);
        await SeedTasksAsync([.. userTasks, otherTask]);

        _output.WriteLine($"Target UserId : {_userId}");
        _output.WriteLine($"Expected Count: {userTasks.Count}");
        userTasks.ForEach(t => _output.WriteLine($"  User Task : {t.Id} | {t.Title}"));
        _output.WriteLine($"  Other Task: {otherTask.Id} | UserId: {otherTask.UserId}");

        // Act
        var actual = await _repository.GetUserTasksAsync(_userId);
        _output.WriteLine($"Actual Count: {actual.Count}");
        actual.ToList().ForEach(t => _output.WriteLine($"  Returned: {t.Id} | UserId: {t.UserId}"));

        // Assert
        actual.Should().HaveCount(2);
        actual.Should().OnlyContain(t => t.UserId == _userId);
    }

    [Fact]
    public async Task GetUserTasksAsync_UserWithNoTasks_ShouldReturnEmpty()
    {
        // Arrange
        _output.WriteLine($"Target UserId : {_userId}");
        _output.WriteLine($"Expected Count: 0");

        // Act
        var actual = await _repository.GetUserTasksAsync(_userId);
        _output.WriteLine($"Actual Count: {actual.Count}");

        // Assert
        actual.Should().BeEmpty();
    }

    [Fact]
    public async Task GetUserTasksAsync_ShouldReturnReadOnlyList()
    {
        // Arrange
        var task = CreateTask(userId: _userId);
        await SeedTasksAsync(task);
        _output.WriteLine($"Seeded Task: {task.Id} | {task.Title}");

        // Act
        var actual = await _repository.GetUserTasksAsync(_userId);
        _output.WriteLine($"Actual Type: {actual.GetType().Name}");

        // Assert
        actual.Should().BeAssignableTo<IReadOnlyList<AppTask>>();
    }

    #endregion

    #region UpdateStatusAsync

    [Fact]
    public async Task UpdateStatusAsync_ValidTask_ShouldReturnTaskWithUpdatedStatus()
    {
        // Arrange
        var task = CreateTask(status: TaskStatusEnum.Pending);
        var newStatus = TaskStatusEnum.Pending;
        await SeedTasksAsync(task);

        _output.WriteLine($"Task Id        : {task.Id}");
        _output.WriteLine($"Initial Status : {task.Status}");
        _output.WriteLine($"Expected Status: {newStatus}");

        // Act
        var actual = await _repository.UpdateStatusAsync(task.Id, newStatus);
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
        var newStatus = TaskStatusEnum.Pending;
        await SeedTasksAsync(task);

        _output.WriteLine($"Task Id        : {task.Id}");
        _output.WriteLine($"Expected Status: {newStatus}");

        // Act
        await _repository.UpdateStatusAsync(task.Id, newStatus);
        await _repository.SaveChangesAsync();

        // Assert — re-fetch via GetUserTasksAsync to confirm persistence
        var persisted = await _repository.GetUserTasksAsync(_userId);
        var actual = persisted.SingleOrDefault(t => t.Id == task.Id);
        _output.WriteLine($"Actual Status (after save): {actual?.Status}");

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
        var actual = await _repository.UpdateStatusAsync(nonExistentId, TaskStatusEnum.Pending);
        _output.WriteLine($"Actual: {actual?.ToString() ?? "null"}");

        // Assert
        actual.Should().BeNull();
    }

    [Fact]
    public async Task UpdateStatusAsync_ShouldNotAffectOtherTasks()
    {
        // Arrange
        var task1 = CreateTask(status: TaskStatusEnum.Pending);
        var task2 = CreateTask(status: TaskStatusEnum.Pending);
        await SeedTasksAsync(task1, task2);

        _output.WriteLine($"Updating Task1: {task1.Id} to Completed");
        _output.WriteLine($"Task2: {task2.Id} should stay Pending");

        // Act
        await _repository.UpdateStatusAsync(task1.Id, TaskStatusEnum.Completed);
        await _repository.SaveChangesAsync();

        // Assert — verify task2 via GetUserTasksAsync
        var tasks = await _repository.GetUserTasksAsync(_userId);
        var actual = tasks.Single(t => t.Id == task2.Id);

        _output.WriteLine($"Task2 Expected: {TaskStatusEnum.Pending}");
        _output.WriteLine($"Task2 Actual  : {actual.Status}");

        actual.Status.Should().Be(TaskStatusEnum.Pending);
    }

    #endregion

    #region SaveChangesAsync

    [Fact]
    public async Task SaveChangesAsync_WithPendingChanges_ShouldReturnTrue()
    {
        // Arrange
        var task = CreateTask();
        _repository.Add(task);
        _output.WriteLine($"Added Task: {task.Id} | {task.Title}");

        // Act
        var actual = await _repository.SaveChangesAsync();
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
        var actual = await _repository.SaveChangesAsync();
        _output.WriteLine($"Expected: false | Actual: {actual}");

        // Assert
        actual.Should().BeFalse();
    }

    #endregion

    #region Helpers

    private AppTask CreateTask(
        Guid? userId = null,
        TaskStatusEnum? status = null)
    {
        return _fixture.Build<AppTask>()
            .With(t => t.UserId, userId ?? _userId)
            .With(t => t.ManagerId, _managerId)
            .With(t => t.Status, status ?? _fixture.Create<TaskStatusEnum>())
            .Without(t => t.User)
            .Without(t => t.Manager)
            .Create();
    }

    private List<AppTask> CreateMany(
        int count,
        Guid? userId = null,
        TaskStatusEnum? status = null)
    {
        return Enumerable
            .Range(0, count)
            .Select(_ => CreateTask(userId, status))
            .ToList();
    }

    private async Task SeedTasksAsync(params AppTask[] tasks)
    {
        await _dbContext.Tasks.AddRangeAsync(tasks);
        await _dbContext.SaveChangesAsync();
    }

    public void Dispose() => _dbContext.Dispose();

    #endregion
}