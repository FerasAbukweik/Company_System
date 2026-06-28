using AutoFixture;
using FluentAssertions;
using Company_System.Infrastructure;
using HR_System.Core.Domain.Entities;
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
    private readonly IFixture _fixture;
    private readonly ITestOutputHelper _output;

    private readonly Guid _userId = Guid.NewGuid();
    private readonly Guid _managerId = Guid.NewGuid();

    public TasksRepositoryTests(ITestOutputHelper output)
    {
        _output = output;

        _fixture = new Fixture();
        _fixture.Behaviors
            .OfType<ThrowingRecursionBehavior>()
            .ToList()
            .ForEach(b => _fixture.Behaviors.Remove(b));
        _fixture.Behaviors.Add(new OmitOnRecursionBehavior());

        var dbOptions = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        _dbContext = new ApplicationDbContext(dbOptions);
        _tasksRepository = new TasksesRepository(_dbContext);
    }

    #region GetUserTasksAsync

    [Fact]
    public async Task GetUserTasksAsync_UserWithTasks_ShouldReturnOnlyUserTasks()
    {
        // Arrange
        var userTasks = CreateMany(5, userId: _userId);
        var otherTasks = CreateMany(5); // belong to random userIds
        await SeedAsync([.. userTasks, .. otherTasks]);

        _output.WriteLine($"UserId: {_userId}");
        _output.WriteLine($"Expected Count: {userTasks.Count}");
        userTasks.ForEach(t => _output.WriteLine($"  Expected: {t.Id} | UserId: {t.UserId}"));

        // Act
        var actual = await _tasksRepository.GetUserTasksAsync(_userId);
        _output.WriteLine($"Actual Count: {actual.Count}");
        actual.ToList().ForEach(t => _output.WriteLine($"  Actual: {t.Id} | UserId: {t.UserId}"));

        // Assert
        actual.Should().NotBeNull();
        actual.Should().HaveCount(userTasks.Count);
        actual.Should().BeEquivalentTo(userTasks);
    }

    [Fact]
    public async Task GetUserTasksAsync_UserWithNoTasks_ShouldReturnEmpty()
    {
        // Arrange — seed tasks for other users only
        var otherTasks = CreateMany(5);
        await SeedAsync([.. otherTasks]);

        _output.WriteLine($"UserId: {_userId}");
        _output.WriteLine($"Expected Count: 0");

        // Act
        var actual = await _tasksRepository.GetUserTasksAsync(_userId);
        _output.WriteLine($"Actual Count: {actual.Count}");

        // Assert
        actual.Should().NotBeNull();
        actual.Should().BeEmpty();
    }

    #endregion

    #region Add

    [Fact]
    public async Task Add_ValidTask_ShouldPersistAfterSave()
    {
        // Arrange
        var task = CreateTask(userId: _userId);
        _output.WriteLine($"Adding Task: {task.Id} | {task.Title}");

        // Act
        _tasksRepository.Add(task);
        await _tasksRepository.SaveChangesAsync();

        // Assert
        var actual = await _tasksRepository.GetUserTasksAsync(_userId);
        _output.WriteLine($"Actual Count: {actual.Count}");
        _output.WriteLine($"Actual Task : {actual.FirstOrDefault()?.Id}");

        actual.Should().ContainSingle(t => t.Id == task.Id);
    }

    [Fact]
    public async Task Add_WithoutSaving_ShouldNotPersist()
    {
        // Arrange
        var task = CreateTask(userId: _userId);
        _output.WriteLine($"Adding Task without saving: {task.Id}");

        // Act — skip SaveChangesAsync intentionally
        _tasksRepository.Add(task);

        // Assert
        var actual = await _tasksRepository.GetUserTasksAsync(_userId);
        _output.WriteLine($"Actual Count: {actual.Count}");

        actual.Should().BeEmpty();
    }

    #endregion

    #region UpdateStatusAsync

    [Fact]
    public async Task UpdateStatusAsync_ValidTask_ShouldReturnTaskWithNewStatus()
    {
        // Arrange
        var task = CreateTask(userId: _userId, status: TaskStatusEnum.Pending);
        await SeedAsync(task);

        _output.WriteLine($"Task Id        : {task.Id}");
        _output.WriteLine($"Initial Status : {task.Status}");
        _output.WriteLine($"Expected Status: {TaskStatusEnum.Pending}");

        // Act
        var actual = await _tasksRepository.UpdateStatusAsync(task.Id, TaskStatusEnum.Pending);
        _output.WriteLine($"Actual Status: {actual?.Status}");

        // Assert
        actual.Should().NotBeNull();
        actual!.Status.Should().Be(TaskStatusEnum.Pending);
    }

    [Fact]
    public async Task UpdateStatusAsync_ValidTask_ShouldPersistAfterSave()
    {
        // Arrange
        var task = CreateTask(userId: _userId, status: TaskStatusEnum.Pending);
        await SeedAsync(task);

        _output.WriteLine($"Task Id        : {task.Id}");
        _output.WriteLine($"Expected Status: {TaskStatusEnum.Pending}");

        // Act
        await _tasksRepository.UpdateStatusAsync(task.Id, TaskStatusEnum.Pending);
        await _tasksRepository.SaveChangesAsync();

        // Assert — re-fetch via GetUserTasksAsync
        var tasks = await _tasksRepository.GetUserTasksAsync(_userId);
        var actual = tasks.Single(t => t.Id == task.Id);
        _output.WriteLine($"Actual Status: {actual.Status}");

        actual.Status.Should().Be(TaskStatusEnum.Pending);
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

        _output.WriteLine($"Updating Task1: {task1.Id} → Completed");
        _output.WriteLine($"Task2 should stay Pending: {task2.Id}");

        // Act
        await _tasksRepository.UpdateStatusAsync(task1.Id, TaskStatusEnum.Completed);
        await _tasksRepository.SaveChangesAsync();

        // Assert
        var tasks = await _tasksRepository.GetUserTasksAsync(_userId);
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
        var task = CreateTask(userId: _userId);
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
            .With(t => t.ManagerId, _managerId)
            .With(t => t.Status, status ?? _fixture.Create<TaskStatusEnum>())
            .Without(t => t.User)
            .Without(t => t.Manager)
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