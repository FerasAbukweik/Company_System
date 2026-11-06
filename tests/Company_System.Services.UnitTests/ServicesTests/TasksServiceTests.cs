using System.Net;
using AutoFixture;
using FluentAssertions;
using HR_System.Core.Abstraction.RepositoryContracts;
using HR_System.Core.Abstraction.ServiceContracts;
using HR_System.Core.Domain.Entities;
using HR_System.Core.DTO.Activity;
using HR_System.Core.DTO.LazyLoading;
using HR_System.Core.DTO.Task;
using HR_System.Core.Enums;
using HR_System.Core.Result;
using HR_System.Infrastructure.Services;
using Moq;
using Xunit.Abstractions;

namespace HR_System.Core.UnitTests.ServicesTests;

public class TasksServiceTests : ServiceTestBase
{
    private readonly ITasksService _tasksService;
    private readonly Mock<ITasksRepository> _tasksRepositoryMock;
    private readonly Mock<IActivitiesService> _activitiesServiceMock;
    private readonly Mock<IClaimsService> _claimsServiceMock;
    private readonly Mock<IOrganizationHierarchyService> _hierarchyServiceMock;

    public TasksServiceTests(ITestOutputHelper output) : base(output)
    {
        _tasksRepositoryMock = new Mock<ITasksRepository>();
        _activitiesServiceMock = new Mock<IActivitiesService>();
        _claimsServiceMock = new Mock<IClaimsService>();
        _hierarchyServiceMock = new Mock<IOrganizationHierarchyService>();
        _claimsServiceMock.Setup(c => c.GetUserName())
            .Returns("test-user");
        _tasksService = new TasksService(_tasksRepositoryMock.Object, _activitiesServiceMock.Object, _hierarchyServiceMock.Object);
    }

    private TaskAddDTO CreateTaskAddDto(Guid? userId = null)
    {
        return Fixture.Build<TaskAddDTO>()
            .With(t => t.UserId, userId ?? Guid.NewGuid())
            .With(
            t => t.Deadline,
            DateTime.UtcNow.AddDays(5)
        )
            .Create();
    }

    private AppTask CreateAppTask(Guid? userId = null, Guid? managerId = null, TaskStatusEnum? status = null)
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
            .Without(t => t.User)
            .Without(t => t.Manager)
            .Without(t => t.Approvals)
            .Create();
    }

    private LazyDTO CreateLazyDto(int taken = 0, int sectionSize = 10)
    {
        return new LazyDTO
        {
            Taken = taken,
            SectionSize = sectionSize
        };
    }

    #region AddAsync
    [Fact]
    public async Task AddAsync_ShouldReturnMappedFailure_WhenGetParentUserIdsFails()
    {
        // Arrange
        var dto = CreateTaskAddDto();
        var currUserId = Guid.NewGuid();
        _hierarchyServiceMock.Setup(h => h.GetParentUserIds(dto.UserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<IReadOnlyList<Guid>>.Failure("failed to get parents", HttpStatusCode.BadRequest));
        LogArrange(nameof(dto), dto);

        // Act
        var result = await _tasksService.AddAsync(dto, currUserId);
        LogAct(result);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Be("failed to get parents");
        result.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        _tasksRepositoryMock.Verify(r => r.Add(It.IsAny<AppTask>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task AddAsync_ShouldReturnForbidden_WhenCurrUserIsNotAParent()
    {
        // Arrange
        var dto = CreateTaskAddDto();
        var currUserId = Guid.NewGuid();
        var parentIds = new List<Guid>
        {
            Guid.NewGuid(),
            Guid.NewGuid()
        };
        _hierarchyServiceMock.Setup(h => h.GetParentUserIds(dto.UserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<IReadOnlyList<Guid>>.Success(parentIds));
        LogArrange(nameof(dto), dto);

        // Act
        var result = await _tasksService.AddAsync(dto, currUserId);
        LogAct(result);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Be("Unauthorized");
        result.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        _tasksRepositoryMock.Verify(r => r.Add(It.IsAny<AppTask>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task AddAsync_ShouldCallRepositoryAdd_WithCorrectData_WhenCurrUserIsAParent()
    {
        // Arrange
        var dto = CreateTaskAddDto();
        var currUserId = Guid.NewGuid();
        var parentIds = new List<Guid>
        {
            currUserId,
            Guid.NewGuid()
        };
        _hierarchyServiceMock.Setup(h => h.GetParentUserIds(dto.UserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<IReadOnlyList<Guid>>.Success(parentIds));
        _activitiesServiceMock.Setup(a => a.AddAsync(It.IsAny<ActivityAddDto>(), currUserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<ActivityDto>.Success(Fixture.Create<ActivityDto>()));
        LogArrange(nameof(dto), dto);

        // Act
        await _tasksService.AddAsync(dto, currUserId);
        LogAct("Operation completed");

        // Assert
        _tasksRepositoryMock.Verify(
            r => r.Add(
                It.Is<AppTask>(t => t.ManagerId == currUserId && t.UserId == dto.UserId && t.Title == dto.Title && t.Description == dto.Description && t.Priority == dto.Priority && t.Deadline == dto.Deadline),
                It.IsAny<CancellationToken>()
            ),
            Times.Once
        );
    }

    [Fact]
    public async Task AddAsync_ShouldReturnMappedFailure_WhenActivityAdditionFails()
    {
        // Arrange
        var dto = CreateTaskAddDto();
        var currUserId = Guid.NewGuid();
        var parentIds = new List<Guid>
        {
            currUserId
        };
        _hierarchyServiceMock.Setup(h => h.GetParentUserIds(dto.UserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<IReadOnlyList<Guid>>.Success(parentIds));
        _activitiesServiceMock.Setup(a => a.AddAsync(It.IsAny<ActivityAddDto>(), currUserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<ActivityDto>.Failure("activity add failed", HttpStatusCode.BadRequest));
        LogArrange(nameof(dto), dto);

        // Act
        var result = await _tasksService.AddAsync(dto, currUserId);
        LogAct(result);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Be("activity add failed");
        result.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task AddAsync_ShouldReturnSuccessWithMappedTask_WhenAllStepsSucceed()
    {
        // Arrange
        var dto = CreateTaskAddDto();
        var currUserId = Guid.NewGuid();
        var parentIds = new List<Guid>
        {
            currUserId
        };
        _hierarchyServiceMock.Setup(h => h.GetParentUserIds(dto.UserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<IReadOnlyList<Guid>>.Success(parentIds));
        _activitiesServiceMock.Setup(a => a.AddAsync(It.IsAny<ActivityAddDto>(), currUserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<ActivityDto>.Success(Fixture.Create<ActivityDto>()));
        LogArrange(nameof(dto), dto);

        // Act
        var result = await _tasksService.AddAsync(dto, currUserId);
        LogAct(result);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value!.Title.Should().Be(dto.Title);
        result.Value.Description.Should().Be(dto.Description);
        result.Value.Priority.Should().Be(dto.Priority);
        result.Value.Deadline.Should().Be(dto.Deadline);
        result.Value.UserId.Should().Be(dto.UserId);
        result.Value.ManagerId.Should().Be(currUserId);
        result.Value.Status.Should().Be(TaskStatusEnum.Pending);
    }

    [Fact]
    public async Task AddAsync_ShouldUseActivityTypeTaskAdded_AndClaimsServiceUserName()
    {
        // Arrange
        var dto = CreateTaskAddDto();
        var currUserId = Guid.NewGuid();
        var parentIds = new List<Guid>
        {
            currUserId
        };
        _hierarchyServiceMock.Setup(h => h.GetParentUserIds(dto.UserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<IReadOnlyList<Guid>>.Success(parentIds));
        ActivityAddDto? capturedActivity = null;
        _activitiesServiceMock.Setup(a => a.AddAsync(It.IsAny<ActivityAddDto>(), currUserId, It.IsAny<CancellationToken>()))
            .Callback<ActivityAddDto, Guid, CancellationToken>((activity, _, _) => capturedActivity = activity)
            .ReturnsAsync(Result<ActivityDto>.Success(Fixture.Create<ActivityDto>()));
        LogArrange(nameof(dto), dto);

        // Act
        await _tasksService.AddAsync(dto, currUserId);
        LogAct("Operation completed");

        // Assert
        capturedActivity.Should().NotBeNull();
        capturedActivity!.Type.Should().Be(ActivityTypeEnum.TaskAdded);
        capturedActivity.TaskId.Should().NotBeNull();
        capturedActivity.ApprovalId.Should().BeNull();
        _claimsServiceMock.Verify(c => c.GetUserName(), Times.Never);
    }

    #endregion
    #region UpdateStatusAsync
    [Fact]
    public async Task UpdateStatusAsync_ShouldReturnFailure_WhenTaskNotFound()
    {
        // Arrange
        LogArrange("Scenario", nameof(UpdateStatusAsync_ShouldReturnFailure_WhenTaskNotFound));

        var taskId = Guid.NewGuid();
        var currentUserId = Guid.NewGuid();
        _tasksRepositoryMock.Setup(r => r.UpdateStatusAsync(taskId, It.IsAny<TaskStatusEnum>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((AppTask? )null);
        _tasksRepositoryMock.Setup(r => r.GetByIdAsync(taskId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((AppTask? )null);

        // Act
        var result = await _tasksService.UpdateStatusAsync(taskId, TaskStatusEnum.Completed, currentUserId);
        LogAct(result);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Be("Failed to update task or task doesnt exist");
        _tasksRepositoryMock.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task UpdateStatusAsync_ShouldReturnForbidden_WhenCurrentUserIsNeitherOwnerNorManager()
    {
        // Arrange
        LogArrange(
            "Scenario",
            nameof(UpdateStatusAsync_ShouldReturnForbidden_WhenCurrentUserIsNeitherOwnerNorManager)
        );
        var taskId = Guid.NewGuid();
        var currentUserId = Guid.NewGuid();
        var task = CreateAppTask(userId: Guid.NewGuid(), managerId: Guid.NewGuid());
        _tasksRepositoryMock.Setup(r => r.UpdateStatusAsync(taskId, It.IsAny<TaskStatusEnum>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(task);
        _tasksRepositoryMock.Setup(r => r.GetByIdAsync(taskId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(task);

        // Act
        var result = await _tasksService.UpdateStatusAsync(taskId, TaskStatusEnum.Completed, currentUserId);
        LogAct(result);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Be("Unauthorized");
        result.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        _tasksRepositoryMock.Verify(
            repository => repository.SaveChangesAsync(It.IsAny<CancellationToken>()),
            Times.Never
        );
    }

    [Theory]
    [InlineData(TaskStatusEnum.Rejected)]
    [InlineData(TaskStatusEnum.Pending)]
    public async Task UpdateStatusAsync_ShouldReturnForbidden_WhenOwnerTriesToSetRejectedOrPending(TaskStatusEnum newStatus)
    {
        // Arrange
        var taskId = Guid.NewGuid();
        var currentUserId = Guid.NewGuid();
        // current user is the owner (UserId) but NOT the manager
        var task = CreateAppTask(userId: currentUserId, managerId: Guid.NewGuid());
        _tasksRepositoryMock.Setup(r => r.UpdateStatusAsync(taskId, newStatus, It.IsAny<CancellationToken>()))
            .ReturnsAsync(task);
        _tasksRepositoryMock.Setup(r => r.GetByIdAsync(taskId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(task);
        LogArrange(nameof(newStatus), newStatus);

        // Act
        var result = await _tasksService.UpdateStatusAsync(taskId, newStatus, currentUserId);
        LogAct(result);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Be("Unauthorized");
        result.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        _tasksRepositoryMock.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Theory]
    [InlineData(TaskStatusEnum.Rejected)]
    [InlineData(TaskStatusEnum.Pending)]
    public async Task UpdateStatusAsync_ShouldAllowManager_ToSetRejectedOrPending(TaskStatusEnum newStatus)
    {
        // Arrange
        var taskId = Guid.NewGuid();
        var currentUserId = Guid.NewGuid();
        var task = CreateAppTask(userId: Guid.NewGuid(), managerId: currentUserId, status: newStatus);
        _tasksRepositoryMock.Setup(r => r.UpdateStatusAsync(taskId, newStatus, It.IsAny<CancellationToken>()))
            .ReturnsAsync(task);
        _tasksRepositoryMock.Setup(r => r.GetByIdAsync(taskId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(task);
        _tasksRepositoryMock.Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        LogArrange(nameof(newStatus), newStatus);

        // Act
        var result = await _tasksService.UpdateStatusAsync(taskId, newStatus, currentUserId);
        LogAct(result);

        // Assert
        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task UpdateStatusAsync_ShouldAllowOwner_ToSetCompleted()
    {
        // Arrange
        LogArrange("Scenario", nameof(UpdateStatusAsync_ShouldAllowOwner_ToSetCompleted));

        var taskId = Guid.NewGuid();
        var currentUserId = Guid.NewGuid();
        var task = CreateAppTask(userId: currentUserId, managerId: Guid.NewGuid(), status: TaskStatusEnum.Completed);
        _tasksRepositoryMock.Setup(r => r.UpdateStatusAsync(taskId, TaskStatusEnum.Completed, It.IsAny<CancellationToken>()))
            .ReturnsAsync(task);
        _tasksRepositoryMock.Setup(r => r.GetByIdAsync(taskId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(task);
        _tasksRepositoryMock.Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        // Act
        var result = await _tasksService.UpdateStatusAsync(taskId, TaskStatusEnum.Completed, currentUserId);
        LogAct(result);

        // Assert
        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task UpdateStatusAsync_ShouldReturnFailure_WhenSaveChangesFails()
    {
        // Arrange
        LogArrange("Scenario", nameof(UpdateStatusAsync_ShouldReturnFailure_WhenSaveChangesFails));

        var taskId = Guid.NewGuid();
        var currentUserId = Guid.NewGuid();
        var task = CreateAppTask(userId: currentUserId, managerId: currentUserId, status: TaskStatusEnum.Completed);
        _tasksRepositoryMock.Setup(r => r.UpdateStatusAsync(taskId, TaskStatusEnum.Completed, It.IsAny<CancellationToken>()))
            .ReturnsAsync(task);
        _tasksRepositoryMock.Setup(r => r.GetByIdAsync(taskId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(task);
        _tasksRepositoryMock.Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        // Act
        var result = await _tasksService.UpdateStatusAsync(taskId, TaskStatusEnum.Completed, currentUserId);
        LogAct(result);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Be("Failed to update task or task doesnt exist");
    }

    [Fact]
    public async Task UpdateStatusAsync_ShouldReturnSuccessWithNoContent_WhenAllStepsSucceed()
    {
        // Arrange
        LogArrange("Scenario", nameof(UpdateStatusAsync_ShouldReturnSuccessWithNoContent_WhenAllStepsSucceed));

        var taskId = Guid.NewGuid();
        var currentUserId = Guid.NewGuid();
        var task = CreateAppTask(userId: currentUserId, managerId: currentUserId, status: TaskStatusEnum.Completed);
        _tasksRepositoryMock.Setup(r => r.UpdateStatusAsync(taskId, TaskStatusEnum.Completed, It.IsAny<CancellationToken>()))
            .ReturnsAsync(task);
        _tasksRepositoryMock.Setup(r => r.GetByIdAsync(taskId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(task);
        _tasksRepositoryMock.Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        // Act
        var result = await _tasksService.UpdateStatusAsync(taskId, TaskStatusEnum.Completed, currentUserId);
        LogAct(result);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.StatusCode.Should().Be(HttpStatusCode.NoContent);
        result.Value.Should().NotBeNull();
        result.Value!.Id.Should().Be(task.Id);
        result.Value.Status.Should().Be(TaskStatusEnum.Completed);
        result.Value.ManagerId.Should().Be(currentUserId);
    }

    #endregion
    #region LazyGetUserTasksAsync
    [Fact]
    public async Task LazyGetUserTasksAsync_ShouldReturnFailure_WhenTakenIsNegative()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var lazyData = CreateLazyDto(taken: -1);
        LogArrange(nameof(userId), userId);

        // Act
        var result = await _tasksService.LazyGetUserTasksAsync(userId, lazyData);
        LogAct(result);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Be("Taken cannot be negative");
        result.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        _tasksRepositoryMock.Verify(
            r => r.FilterAsync(
                It.IsAny<System.Linq.Expressions.Expression<Func<AppTask, bool>>>(),
                It.IsAny<System.Linq.Expressions.Expression<Func<AppTask, object?>>[]?>(),
                It.IsAny<System.Linq.Expressions.Expression<Func<AppTask, object?>>?>(),
                It.IsAny<bool>(),
                It.IsAny<int?>(),
                It.IsAny<int?>(),
                It.IsAny<CancellationToken>()
            ),
            Times.Never
        );
    }

    [Fact]
    public async Task LazyGetUserTasksAsync_ShouldReturnMappedTasks_WhenTasksExist()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var lazyData = CreateLazyDto();
        var tasks = new List<AppTask>
        {
            CreateAppTask(),
            CreateAppTask()
        };
        _tasksRepositoryMock.Setup(r => r.FilterAsync(
            It.IsAny<System.Linq.Expressions.Expression<Func<AppTask, bool>>>(),
            It.IsAny<System.Linq.Expressions.Expression<Func<AppTask, object?>>[]?>(),
            It.IsAny<System.Linq.Expressions.Expression<Func<AppTask, object?>>?>(),
            It.IsAny<bool>(),
            It.IsAny<int?>(),
            It.IsAny<int?>(),
            It.IsAny<CancellationToken>()
        ))
            .ReturnsAsync(tasks);
        LogArrange(nameof(userId), userId);

        // Act
        var result = await _tasksService.LazyGetUserTasksAsync(userId, lazyData);
        LogAct(result);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value!.Count.Should().Be(tasks.Count);
    }

    [Fact]
    public async Task LazyGetUserTasksAsync_ShouldReturnEmptyList_WhenNoTasksExist()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var lazyData = CreateLazyDto();
        _tasksRepositoryMock.Setup(r => r.FilterAsync(
            It.IsAny<System.Linq.Expressions.Expression<Func<AppTask, bool>>>(),
            It.IsAny<System.Linq.Expressions.Expression<Func<AppTask, object?>>[]?>(),
            It.IsAny<System.Linq.Expressions.Expression<Func<AppTask, object?>>?>(),
            It.IsAny<bool>(),
            It.IsAny<int?>(),
            It.IsAny<int?>(),
            It.IsAny<CancellationToken>()
        ))
            .ReturnsAsync(new List<AppTask>());
        LogArrange(nameof(userId), userId);

        // Act
        var result = await _tasksService.LazyGetUserTasksAsync(userId, lazyData);
        LogAct(result);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeEmpty();
    }

    [Fact]
    public async Task LazyGetUserTasksAsync_ShouldCallRepository_WithCorrectParameters()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var lazyData = CreateLazyDto();
        _tasksRepositoryMock.Setup(r => r.FilterAsync(
            It.IsAny<System.Linq.Expressions.Expression<Func<AppTask, bool>>>(),
            It.IsAny<System.Linq.Expressions.Expression<Func<AppTask, object?>>[]?>(),
            It.IsAny<System.Linq.Expressions.Expression<Func<AppTask, object?>>?>(),
            It.IsAny<bool>(),
            It.IsAny<int?>(),
            It.IsAny<int?>(),
            It.IsAny<CancellationToken>()
        ))
            .ReturnsAsync(new List<AppTask>());
        LogArrange(nameof(userId), userId);

        // Act
        await _tasksService.LazyGetUserTasksAsync(userId, lazyData);
        LogAct("Operation completed");

        // Assert
        _tasksRepositoryMock.Verify(
            r => r.FilterAsync(
                It.IsAny<System.Linq.Expressions.Expression<Func<AppTask, bool>>>(),
                It.IsAny<System.Linq.Expressions.Expression<Func<AppTask, object?>>[]?>(),
                It.IsAny<System.Linq.Expressions.Expression<Func<AppTask, object?>>?>(),
                It.IsAny<bool>(),
                It.IsAny<int?>(),
                It.IsAny<int?>(),
                It.IsAny<CancellationToken>()
            ),
            Times.Once
        );
    }
    #endregion
}
