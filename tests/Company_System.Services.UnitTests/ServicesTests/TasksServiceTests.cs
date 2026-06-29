using System.Net;
using AutoFixture;
using FluentAssertions;
using HR_System.Core.common;
using HR_System.Core.Domain.Entities;
using HR_System.Core.DTO.Activity;
using HR_System.Core.DTO.LazyLoading;
using HR_System.Core.DTO.Task;
using HR_System.Core.Enums;
using HR_System.Core.Interfaces.RepositoryContracts;
using HR_System.Core.Interfaces.ServiceContracts.IActivitiesService;
using HR_System.Infrastructure.Services;
using Moq;
using Xunit.Abstractions;

namespace HR_System.Core.UnitTests.ServicesTests;

public class TasksServiceTests
{
    private readonly ITestOutputHelper _output;
    private readonly IFixture _fixture;
    private readonly Mock<ITasksRepository> _tasksRepositoryMock;
    private readonly TasksService _tasksService;
    private readonly Mock<IActivitiesService> _activitiesServiceMock;

    public TasksServiceTests(ITestOutputHelper output)
    {
        _output = output;

        _fixture = new Fixture();
        _fixture.Behaviors.OfType<ThrowingRecursionBehavior>().ToList()
            .ForEach(b => _fixture.Behaviors.Remove(b));
        _fixture.Behaviors.Add(new OmitOnRecursionBehavior());

        _activitiesServiceMock = new Mock<IActivitiesService>();
        _tasksRepositoryMock = new Mock<ITasksRepository>();
        _tasksService = new TasksService(_tasksRepositoryMock.Object, _activitiesServiceMock.Object);
    }

    #region AddAsync

    [Fact]
    public async Task AddAsync_ValidData_ShouldSucceed()
    {
        // Arrange
        var currUserId = Guid.NewGuid();
        var toAdd = _fixture.Create<TaskAddDTO>();

        _activitiesServiceMock.Setup(t =>
                t.AddAsync(It.IsAny<ActivityAddDTO>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<ActivityDTO>.Success(_fixture.Create<ActivityDTO>()));
        _tasksRepositoryMock
            .Setup(r => r.Add(It.IsAny<AppTask>(), It.IsAny<CancellationToken>()));
        _tasksRepositoryMock
            .Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        _output.WriteLine($"ManagerId (currUserId): {currUserId}");
        _output.WriteLine($"UserId   : {toAdd.UserId}");
        _output.WriteLine($"Title    : {toAdd.Title}");
        _output.WriteLine($"Priority : {toAdd.Priority}");

        // Act
        var actual = await _tasksService.AddAsync(toAdd, currUserId);
        _output.WriteLine($"IsSuccess       : {actual.IsSuccess}");
        _output.WriteLine($"Actual Title    : {actual.Value?.Title}");
        _output.WriteLine($"Actual UserId   : {actual.Value?.UserId}");
        _output.WriteLine($"Actual ManagerId: {actual.Value?.ManagerId}");

        // Assert
        actual.Should().NotBeNull();
        actual.IsSuccess.Should().BeTrue();
        actual.Value.Should().NotBeNull();
        actual.Value!.Title.Should().Be(toAdd.Title);
        actual.Value!.Description.Should().Be(toAdd.Description);
        actual.Value!.Priority.Should().Be(toAdd.Priority);
        actual.Value!.UserId.Should().Be(toAdd.UserId);
        actual.Value!.ManagerId.Should().Be(currUserId);

        _tasksRepositoryMock.Verify(r =>
            r.Add(It.IsAny<AppTask>(), It.IsAny<CancellationToken>()), Times.Once);
        _tasksRepositoryMock.Verify(r =>
            r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task AddAsync_SaveChangesFails_ShouldReturnFailure()
    {
        // Arrange
        var currUserId = Guid.NewGuid();
        var toAdd = _fixture.Create<TaskAddDTO>();

        _activitiesServiceMock.Setup(t =>
                t.AddAsync(It.IsAny<ActivityAddDTO>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<ActivityDTO>.Success(_fixture.Create<ActivityDTO>()));
        _tasksRepositoryMock
            .Setup(r => r.Add(It.IsAny<AppTask>(), It.IsAny<CancellationToken>()));
        _tasksRepositoryMock
            .Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        _output.WriteLine($"currUserId: {currUserId}");
        _output.WriteLine("SaveChanges returns false — expecting failure");

        // Act
        var actual = await _tasksService.AddAsync(toAdd, currUserId);
        _output.WriteLine($"IsSuccess    : {actual.IsSuccess}");
        _output.WriteLine($"ErrorMessage : {actual.ErrorMessage}");

        // Assert
        actual.Should().NotBeNull();
        actual.IsSuccess.Should().BeFalse();
        actual.ErrorMessage.Should().Be("Failed to save task");

        _tasksRepositoryMock.Verify(r =>
            r.Add(It.IsAny<AppTask>(), It.IsAny<CancellationToken>()), Times.Once);
        _tasksRepositoryMock.Verify(r =>
            r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    #endregion

    #region LazyGetUserTasksAsync

    [Fact]
    public async Task LazyGetUserTasksAsync_ValidData_ShouldReturnMappedDTOs()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var lazyData = new LazyDTO { Taken = 0, SectionSize = 10 };
        var tasks = _fixture.CreateMany<AppTask>(3).ToList();

        _tasksRepositoryMock
            .Setup(r => r.LazyGetUserTasksAsync(userId, lazyData, It.IsAny<CancellationToken>()))
            .ReturnsAsync(tasks);

        _output.WriteLine($"UserId        : {userId}");
        _output.WriteLine($"Expected Count: {tasks.Count}");
        tasks.ForEach(t => _output.WriteLine($"  Expected: {t.Id} | {t.Title}"));

        // Act
        var actual = await _tasksService.LazyGetUserTasksAsync(userId, lazyData);
        _output.WriteLine($"IsSuccess   : {actual.IsSuccess}");
        _output.WriteLine($"Actual Count: {actual.Value?.Count ?? -1}");
        actual.Value?.ToList().ForEach(t => _output.WriteLine($"  Actual: {t.Id} | {t.Title}"));

        // Assert
        actual.Should().NotBeNull();
        actual.IsSuccess.Should().BeTrue();
        actual.Value.Should().NotBeNull();
        actual.Value.Should().HaveCount(tasks.Count);
        actual.Value.Should().BeAssignableTo<IReadOnlyList<TaskDTO>>();

        _tasksRepositoryMock.Verify(r =>
            r.LazyGetUserTasksAsync(userId, lazyData, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task LazyGetUserTasksAsync_NoTasks_ShouldReturnEmpty()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var lazyData = new LazyDTO { Taken = 0, SectionSize = 10 };

        _tasksRepositoryMock
            .Setup(r => r.LazyGetUserTasksAsync(userId, lazyData, It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);

        _output.WriteLine($"UserId        : {userId}");
        _output.WriteLine("Expected Count: 0");

        // Act
        var actual = await _tasksService.LazyGetUserTasksAsync(userId, lazyData);
        _output.WriteLine($"IsSuccess   : {actual.IsSuccess}");
        _output.WriteLine($"Actual Count: {actual.Value?.Count ?? -1}");

        // Assert
        actual.Should().NotBeNull();
        actual.IsSuccess.Should().BeTrue();
        actual.Value.Should().BeEmpty();

        _tasksRepositoryMock.Verify(r =>
            r.LazyGetUserTasksAsync(userId, lazyData, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task LazyGetUserTasksAsync_NegativeTaken_ShouldReturnFailure()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var lazyData = new LazyDTO { Taken = -1, SectionSize = 10 };

        _output.WriteLine($"UserId: {userId}");
        _output.WriteLine($"Taken : {lazyData.Taken} — expecting BadRequest failure");

        // Act
        var actual = await _tasksService.LazyGetUserTasksAsync(userId, lazyData);
        _output.WriteLine($"IsSuccess    : {actual.IsSuccess}");
        _output.WriteLine($"StatusCode   : {actual.StatusCode}");
        _output.WriteLine($"ErrorMessage : {actual.ErrorMessage}");

        // Assert
        actual.Should().NotBeNull();
        actual.IsSuccess.Should().BeFalse();
        actual.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        actual.ErrorMessage.Should().Be("Taken cannot be negative");

        // Repository should never be called
        _tasksRepositoryMock.Verify(r =>
            r.LazyGetUserTasksAsync(It.IsAny<Guid>(), It.IsAny<LazyDTO>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    #endregion

    #region UpdateStatusAsync

    [Fact]
    public async Task UpdateStatusAsync_ValidData_ShouldSucceed()
    {
        // Arrange
        var currentUserId = Guid.NewGuid();
        var task = CreateTask(userId: currentUserId);
        var newStatus = TaskStatusEnum.Pending;

        _activitiesServiceMock.Setup(t =>
                t.AddAsync(It.IsAny<ActivityAddDTO>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<ActivityDTO>.Success(_fixture.Create<ActivityDTO>()));
        _tasksRepositoryMock
            .Setup(r => r.UpdateStatusAsync(task.Id, newStatus, It.IsAny<CancellationToken>()))
            .ReturnsAsync(task);
        _tasksRepositoryMock
            .Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        _output.WriteLine($"TaskId       : {task.Id}");
        _output.WriteLine($"CurrentUserId: {currentUserId}");
        _output.WriteLine($"New Status   : {newStatus}");

        // Act
        var actual = await _tasksService.UpdateStatusAsync(currentUserId, task.Id, newStatus);
        _output.WriteLine($"IsSuccess    : {actual.IsSuccess}");
        _output.WriteLine($"Actual Status: {actual.Value?.Status}");

        // Assert
        actual.Should().NotBeNull();
        actual.IsSuccess.Should().BeTrue();
        actual.Value.Should().NotBeNull();
        actual.Value!.Id.Should().Be(task.Id);
        actual.Value!.Status.Should().Be(task.Status);

        _tasksRepositoryMock.Verify(r =>
            r.UpdateStatusAsync(task.Id, newStatus, It.IsAny<CancellationToken>()), Times.Once);
        _tasksRepositoryMock.Verify(r =>
            r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task UpdateStatusAsync_TaskNotFound_ShouldReturnFailure()
    {
        // Arrange
        var taskId = Guid.NewGuid();
        var currentUserId = Guid.NewGuid();
        var newStatus = TaskStatusEnum.Pending;

        _activitiesServiceMock.Setup(t =>
                t.AddAsync(It.IsAny<ActivityAddDTO>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<ActivityDTO>.Success(_fixture.Create<ActivityDTO>()));
        _tasksRepositoryMock
            .Setup(r => r.UpdateStatusAsync(taskId, newStatus, It.IsAny<CancellationToken>()))
            .ReturnsAsync(null as AppTask);

        _output.WriteLine($"TaskId: {taskId}");
        _output.WriteLine("UpdateStatus returns null — expecting failure");

        // Act
        var actual = await _tasksService.UpdateStatusAsync(currentUserId, taskId, newStatus);
        _output.WriteLine($"IsSuccess    : {actual.IsSuccess}");
        _output.WriteLine($"ErrorMessage : {actual.ErrorMessage}");

        // Assert
        actual.Should().NotBeNull();
        actual.IsSuccess.Should().BeFalse();
        actual.ErrorMessage.Should().Be("Failed to update task status or task want found");

        _tasksRepositoryMock.Verify(r =>
            r.UpdateStatusAsync(taskId, newStatus, It.IsAny<CancellationToken>()), Times.Once);
        _tasksRepositoryMock.Verify(r =>
            r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task UpdateStatusAsync_WrongUser_ShouldReturnUnauthorized()
    {
        // Arrange
        var currentUserId = Guid.NewGuid();
        var task = CreateTask(userId: Guid.NewGuid()); // different user
        var newStatus = TaskStatusEnum.Pending;

        _activitiesServiceMock.Setup(t =>
                t.AddAsync(It.IsAny<ActivityAddDTO>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<ActivityDTO>.Success(_fixture.Create<ActivityDTO>()));
        _tasksRepositoryMock
            .Setup(r => r.UpdateStatusAsync(task.Id, newStatus, It.IsAny<CancellationToken>()))
            .ReturnsAsync(task);

        _output.WriteLine($"TaskId       : {task.Id}");
        _output.WriteLine($"Task UserId  : {task.UserId}");
        _output.WriteLine($"CurrentUserId: {currentUserId}");
        _output.WriteLine("User mismatch — expecting Unauthorized");

        // Act
        var actual = await _tasksService.UpdateStatusAsync(currentUserId, task.Id, newStatus);
        _output.WriteLine($"IsSuccess    : {actual.IsSuccess}");
        _output.WriteLine($"StatusCode   : {actual.StatusCode}");
        _output.WriteLine($"ErrorMessage : {actual.ErrorMessage}");

        // Assert
        actual.Should().NotBeNull();
        actual.IsSuccess.Should().BeFalse();
        actual.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        actual.ErrorMessage.Should().Be("Unauthorized");

        _tasksRepositoryMock.Verify(r =>
            r.UpdateStatusAsync(task.Id, newStatus, It.IsAny<CancellationToken>()), Times.Once);
        _tasksRepositoryMock.Verify(r =>
            r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task UpdateStatusAsync_SaveChangesFails_ShouldReturnFailure()
    {
        // Arrange
        var currentUserId = Guid.NewGuid();
        var task = CreateTask(userId: currentUserId);
        var newStatus = TaskStatusEnum.Pending;

        _activitiesServiceMock.Setup(t =>
                t.AddAsync(It.IsAny<ActivityAddDTO>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<ActivityDTO>.Success(_fixture.Create<ActivityDTO>()));
        _tasksRepositoryMock
            .Setup(r => r.UpdateStatusAsync(task.Id, newStatus, It.IsAny<CancellationToken>()))
            .ReturnsAsync(task);
        _tasksRepositoryMock
            .Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        _output.WriteLine($"TaskId: {task.Id}");
        _output.WriteLine("SaveChanges returns false — expecting failure");

        // Act
        var actual = await _tasksService.UpdateStatusAsync(currentUserId, task.Id, newStatus);
        _output.WriteLine($"IsSuccess    : {actual.IsSuccess}");
        _output.WriteLine($"ErrorMessage : {actual.ErrorMessage}");

        // Assert
        actual.Should().NotBeNull();
        actual.IsSuccess.Should().BeFalse();
        actual.ErrorMessage.Should().Be("Failed to save task");

        _tasksRepositoryMock.Verify(r =>
            r.UpdateStatusAsync(task.Id, newStatus, It.IsAny<CancellationToken>()), Times.Once);
        _tasksRepositoryMock.Verify(r =>
            r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    #endregion

    #region Helpers

    private AppTask CreateTask(Guid? userId = null) =>
        _fixture.Build<AppTask>()
            .With(t => t.UserId, userId ?? Guid.NewGuid())
            .Create();

    #endregion
}