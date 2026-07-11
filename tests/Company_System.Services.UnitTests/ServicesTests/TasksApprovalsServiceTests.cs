using System.Net;
using AutoFixture;
using FluentAssertions;
using HR_System.Core.common;
using HR_System.Core.Domain.Entities;
using HR_System.Core.Domain.Identity;
using HR_System.Core.DTO.Activity;
using HR_System.Core.DTO.Approval;
using HR_System.Core.DTO.Task;
using HR_System.Core.Enums;
using HR_System.Core.helpers;
using HR_System.Core.Interfaces.RepositoryContracts;
using HR_System.Core.Interfaces.ServiceContracts;
using HR_System.Infrastructure.Services;
using Moq;
using Xunit.Abstractions;

namespace TestProject1.ServicesTests;

public class TasksApprovalsServiceTests
{
    private readonly ITasksApprovalsService _tasksApprovalsService;
    private readonly Mock<ITasksService> _tasksServiceMock;
    private readonly Mock<IApprovalService> _approvalServiceMock;
    private readonly Mock<IActivitiesService> _activitiesServiceMock;
    private readonly Mock<IApprovalRepository> _approvalRepositoryMock;
    private readonly ITestOutputHelper _output;
    private readonly IFixture _fixture;

    public TasksApprovalsServiceTests(ITestOutputHelper output)
    {
        _output = output;

        _fixture = new Fixture();
        _fixture.Behaviors.OfType<ThrowingRecursionBehavior>().ToList()
            .ForEach(b => _fixture.Behaviors.Remove(b));
        _fixture.Behaviors.Add(new OmitOnRecursionBehavior());

        _tasksServiceMock = new Mock<ITasksService>();
        _approvalServiceMock = new Mock<IApprovalService>();
        _activitiesServiceMock = new Mock<IActivitiesService>();
        _approvalRepositoryMock = new Mock<IApprovalRepository>();

        _tasksApprovalsService = new TasksApprovalsService(
            _tasksServiceMock.Object,
            _approvalServiceMock.Object,
            _activitiesServiceMock.Object,
            _approvalRepositoryMock.Object);
    }

    private TaskDTO CreateTaskDto(Guid? managerId = null, TaskStatusEnum? status = null)
    {
        return _fixture.Build<TaskDTO>()
            .With(t => t.ManagerId, managerId ?? Guid.NewGuid())
            .With(t => t.Status, status ?? TaskStatusEnum.Pending)
            .Create();
    }

    private ApplicationUser CreateUser(string? userName = null)
    {
        return _fixture.Build<ApplicationUser>()
            .With(u => u.UserName, userName ?? _fixture.Create<string>())
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

    private AppTask CreateAppTask(string? title = null)
    {
        return _fixture.Build<AppTask>()
            .With(t => t.Title, title ?? _fixture.Create<string>())
            // NOTE: if AppTask has navigation properties (User, Manager, Approvals, etc.)
            // that cause AutoFixture recursion errors, add .Without(...) calls here.
            .Create();
    }

    private Approval CreateApproval(
        Guid? id = null,
        ApprovalTypeEnum? type = null,
        ApprovalStatusEnum? status = null,
        Guid? taskId = null,
        AppTask? task = null,
        Guid? managerId = null,
        ApplicationUser? manager = null,
        Guid? userRequestingId = null,
        ApplicationUser? userRequesting = null)
    {
        return _fixture.Build<Approval>()
            .With(a => a.Id, id ?? Guid.NewGuid())
            .With(a => a.Type, type ?? ApprovalTypeEnum.Task)
            .With(a => a.Status, status ?? ApprovalStatusEnum.Pending)
            .With(a => a.TaskId, taskId)
            .With(a => a.Task, task)
            .With(a => a.ManagerId, managerId ?? Guid.NewGuid())
            .With(a => a.Manager, manager)
            .With(a => a.UserRequestingId, userRequestingId ?? Guid.NewGuid())
            .With(a => a.UserRequesting, userRequesting)
            .Create();
    }

    #region UpdateTaskStatusAsync

    [Fact]
    public async Task UpdateTaskStatusAsync_ShouldReturnFailure_WhenUpdateStatusFails()
    {
        // Arrange
        var currentUserId = Guid.NewGuid();
        var taskId = Guid.NewGuid();

        _tasksServiceMock
            .Setup(s => s.UpdateStatusAsync(taskId, TaskStatusEnum.Completed, currentUserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<TaskDTO>.Failure("task not found"));

        // Act
        var result = await _tasksApprovalsService.UpdateTaskStatusAsync(currentUserId, taskId, TaskStatusEnum.Completed);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Be("task not found");

        _approvalServiceMock.Verify(
            a => a.AddAsync(It.IsAny<ApprovalAddDTO>(), It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task UpdateTaskStatusAsync_ShouldNotAddApproval_WhenNewStatusIsNotCompleted()
    {
        // Arrange
        var currentUserId = Guid.NewGuid();
        var taskId = Guid.NewGuid();
        var taskDto = CreateTaskDto(status: TaskStatusEnum.Rejected);

        _tasksServiceMock
            .Setup(s => s.UpdateStatusAsync(taskId, TaskStatusEnum.Rejected, currentUserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<TaskDTO>.Success(taskDto));

        // Act
        var result = await _tasksApprovalsService.UpdateTaskStatusAsync(currentUserId, taskId, TaskStatusEnum.Rejected);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(taskDto);

        _approvalServiceMock.Verify(
            a => a.AddAsync(It.IsAny<ApprovalAddDTO>(), It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task UpdateTaskStatusAsync_ShouldAddApproval_WithCorrectData_WhenNewStatusIsCompleted()
    {
        // Arrange
        var currentUserId = Guid.NewGuid();
        var taskId = Guid.NewGuid();
        var managerId = Guid.NewGuid();
        var taskDto = CreateTaskDto(managerId: managerId, status: TaskStatusEnum.Completed);

        _tasksServiceMock
            .Setup(s => s.UpdateStatusAsync(taskId, TaskStatusEnum.Completed, currentUserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<TaskDTO>.Success(taskDto));

        _approvalServiceMock
            .Setup(a => a.AddAsync(It.IsAny<ApprovalAddDTO>(), currentUserId, managerId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<ToApproveDTO>.Success(_fixture.Create<ToApproveDTO>()));

        // Act
        var result = await _tasksApprovalsService.UpdateTaskStatusAsync(currentUserId, taskId, TaskStatusEnum.Completed);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(taskDto);

        _approvalServiceMock.Verify(
            a => a.AddAsync(
                It.Is<ApprovalAddDTO>(dto => dto.Type == ApprovalTypeEnum.Task && dto.TaskId == taskId),
                currentUserId,
                managerId,
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task UpdateTaskStatusAsync_ShouldReturnMappedFailure_WhenAddApprovalFails()
    {
        // Arrange
        var currentUserId = Guid.NewGuid();
        var taskId = Guid.NewGuid();
        var managerId = Guid.NewGuid();
        var taskDto = CreateTaskDto(managerId: managerId, status: TaskStatusEnum.Completed);

        _tasksServiceMock
            .Setup(s => s.UpdateStatusAsync(taskId, TaskStatusEnum.Completed, currentUserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<TaskDTO>.Success(taskDto));

        _approvalServiceMock
            .Setup(a => a.AddAsync(It.IsAny<ApprovalAddDTO>(), currentUserId, managerId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<ToApproveDTO>.Failure("approval creation failed", HttpStatusCode.BadRequest));

        // Act
        var result = await _tasksApprovalsService.UpdateTaskStatusAsync(currentUserId, taskId, TaskStatusEnum.Completed);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Be("approval creation failed");
        result.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    #endregion

    #region UpdateApprovalStatus

    [Fact]
    public async Task UpdateApprovalStatus_ShouldReturnFailure_WhenApprovalNotFound()
    {
        // Arrange
        var approvalId = Guid.NewGuid();
        var currentUserId = Guid.NewGuid();

        _approvalRepositoryMock
            .Setup(r => r.UpdateStatus(approvalId, It.IsAny<ApprovalStatusEnum>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Approval?)null);

        // Act
        var result = await _tasksApprovalsService.UpdateApprovalStatus(approvalId, ApprovalStatusEnum.Approved, currentUserId);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Be("Failed Updating Approval or Approval Doesnt exist");
        result.StatusCode.Should().Be(HttpStatusCode.InternalServerError);

        _approvalRepositoryMock.Verify(
            r => r.FilterAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Approval, bool>>>(),
                It.IsAny<System.Linq.Expressions.Expression<Func<Approval, object>>[]?>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task UpdateApprovalStatus_ShouldReturnUnauthorized_WhenCurrentUserIsNotTheManager()
    {
        // Arrange
        var approvalId = Guid.NewGuid();
        var currentUserId = Guid.NewGuid();
        var approval = CreateApproval(managerId: Guid.NewGuid());

        _approvalRepositoryMock
            .Setup(r => r.UpdateStatus(approvalId, It.IsAny<ApprovalStatusEnum>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(approval);

        // Act
        var result = await _tasksApprovalsService.UpdateApprovalStatus(approvalId, ApprovalStatusEnum.Approved, currentUserId);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Be("Unauthorized");
        result.StatusCode.Should().Be(HttpStatusCode.Unauthorized);

        _tasksServiceMock.Verify(
            t => t.UpdateStatusAsync(It.IsAny<Guid>(), It.IsAny<TaskStatusEnum>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task UpdateApprovalStatus_ShouldRejectTask_WhenApprovalTypeIsTaskAndNewStatusIsRejected()
    {
        // Arrange
        var approvalId = Guid.NewGuid();
        var currentUserId = Guid.NewGuid();
        var taskId = Guid.NewGuid();
        var approval = CreateApproval(managerId: currentUserId, type: ApprovalTypeEnum.Task, taskId: taskId);
        var updatedIncluded = CreateApproval(id: approval.Id, managerId: currentUserId, type: ApprovalTypeEnum.Task, taskId: taskId,
            task: CreateAppTask(), manager: CreateUser(), userRequesting: CreateUser());

        _approvalRepositoryMock
            .Setup(r => r.UpdateStatus(approvalId, ApprovalStatusEnum.Rejected, It.IsAny<CancellationToken>()))
            .ReturnsAsync(approval);

        _tasksServiceMock
            .Setup(t => t.UpdateStatusAsync(taskId, TaskStatusEnum.Rejected, currentUserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<TaskDTO>.Success(CreateTaskDto()));

        _approvalRepositoryMock
            .Setup(r => r.FilterAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Approval, bool>>>(),
                It.IsAny<System.Linq.Expressions.Expression<Func<Approval, object>>[]?>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Approval> { updatedIncluded });

        _activitiesServiceMock
            .Setup(a => a.AddAsync(It.IsAny<ActivityAddDTO>(), currentUserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<ActivityDTO>.Success(_fixture.Create<ActivityDTO>()));

        // Act
        var result = await _tasksApprovalsService.UpdateApprovalStatus(approvalId, ApprovalStatusEnum.Rejected, currentUserId);

        // Assert
        result.IsSuccess.Should().BeTrue();

        _tasksServiceMock.Verify(
            t => t.UpdateStatusAsync(taskId, TaskStatusEnum.Rejected, currentUserId, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task UpdateApprovalStatus_ShouldReturnMappedFailure_WhenTaskRejectionFails()
    {
        // Arrange
        var approvalId = Guid.NewGuid();
        var currentUserId = Guid.NewGuid();
        var taskId = Guid.NewGuid();
        var approval = CreateApproval(managerId: currentUserId, type: ApprovalTypeEnum.Task, taskId: taskId);

        _approvalRepositoryMock
            .Setup(r => r.UpdateStatus(approvalId, ApprovalStatusEnum.Rejected, It.IsAny<CancellationToken>()))
            .ReturnsAsync(approval);

        _tasksServiceMock
            .Setup(t => t.UpdateStatusAsync(taskId, TaskStatusEnum.Rejected, currentUserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<TaskDTO>.Failure("task update failed", HttpStatusCode.BadRequest));

        // Act
        var result = await _tasksApprovalsService.UpdateApprovalStatus(approvalId, ApprovalStatusEnum.Rejected, currentUserId);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Be("task update failed");
        result.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        _approvalRepositoryMock.Verify(
            r => r.FilterAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Approval, bool>>>(),
                It.IsAny<System.Linq.Expressions.Expression<Func<Approval, object>>[]?>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task UpdateApprovalStatus_ShouldReturnFailure_WhenNoApprovalFoundAfterInclude()
    {
        // Arrange
        var approvalId = Guid.NewGuid();
        var currentUserId = Guid.NewGuid();
        // Holiday type (or Approved status) avoids the task-rejection branch entirely
        var approval = CreateApproval(managerId: currentUserId, type: ApprovalTypeEnum.Holiday);

        _approvalRepositoryMock
            .Setup(r => r.UpdateStatus(approvalId, ApprovalStatusEnum.Approved, It.IsAny<CancellationToken>()))
            .ReturnsAsync(approval);

        _approvalRepositoryMock
            .Setup(r => r.FilterAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Approval, bool>>>(),
                It.IsAny<System.Linq.Expressions.Expression<Func<Approval, object>>[]?>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Approval>());

        // Act
        var result = await _tasksApprovalsService.UpdateApprovalStatus(approvalId, ApprovalStatusEnum.Approved, currentUserId);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Be("no changes happened to DB");
        result.StatusCode.Should().Be(HttpStatusCode.InternalServerError);

        _activitiesServiceMock.Verify(
            a => a.AddAsync(It.IsAny<ActivityAddDTO>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Theory]
    [InlineData(ApprovalStatusEnum.Approved, ActivityTypeEnum.ApprovalApproved)]
    [InlineData(ApprovalStatusEnum.Rejected, ActivityTypeEnum.ApprovalRejected)]
    public async Task UpdateApprovalStatus_ShouldAddActivity_WithCorrectType_BasedOnNewStatus(
        ApprovalStatusEnum newStatus, ActivityTypeEnum expectedActivityType)
    {
        // Arrange
        var approvalId = Guid.NewGuid();
        var currentUserId = Guid.NewGuid();
        // Use Holiday type so the task-rejection branch never triggers, regardless of newStatus
        var approval = CreateApproval(managerId: currentUserId, type: ApprovalTypeEnum.Holiday);
        var updatedIncluded = CreateApproval(id: approval.Id, managerId: currentUserId, type: ApprovalTypeEnum.Holiday,
            manager: CreateUser(), userRequesting: CreateUser());

        _approvalRepositoryMock
            .Setup(r => r.UpdateStatus(approvalId, newStatus, It.IsAny<CancellationToken>()))
            .ReturnsAsync(approval);

        _approvalRepositoryMock
            .Setup(r => r.FilterAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Approval, bool>>>(),
                It.IsAny<System.Linq.Expressions.Expression<Func<Approval, object>>[]?>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Approval> { updatedIncluded });

        _activitiesServiceMock
            .Setup(a => a.AddAsync(It.IsAny<ActivityAddDTO>(), currentUserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<ActivityDTO>.Success(_fixture.Create<ActivityDTO>()));

        // Act
        await _tasksApprovalsService.UpdateApprovalStatus(approvalId, newStatus, currentUserId);

        // Assert
        _activitiesServiceMock.Verify(
            a => a.AddAsync(It.Is<ActivityAddDTO>(dto => dto.Type == expectedActivityType), currentUserId, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task UpdateApprovalStatus_ShouldReturnMappedFailure_WhenActivityAdditionFails()
    {
        // Arrange
        var approvalId = Guid.NewGuid();
        var currentUserId = Guid.NewGuid();
        var approval = CreateApproval(managerId: currentUserId, type: ApprovalTypeEnum.Holiday);
        var updatedIncluded = CreateApproval(id: approval.Id, managerId: currentUserId, type: ApprovalTypeEnum.Holiday,
            manager: CreateUser(), userRequesting: CreateUser());

        _approvalRepositoryMock
            .Setup(r => r.UpdateStatus(approvalId, ApprovalStatusEnum.Approved, It.IsAny<CancellationToken>()))
            .ReturnsAsync(approval);

        _approvalRepositoryMock
            .Setup(r => r.FilterAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Approval, bool>>>(),
                It.IsAny<System.Linq.Expressions.Expression<Func<Approval, object>>[]?>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Approval> { updatedIncluded });

        _activitiesServiceMock
            .Setup(a => a.AddAsync(It.IsAny<ActivityAddDTO>(), currentUserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<ActivityDTO>.Failure("activity creation failed", HttpStatusCode.BadRequest));

        // Act
        var result = await _tasksApprovalsService.UpdateApprovalStatus(approvalId, ApprovalStatusEnum.Approved, currentUserId);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Be("activity creation failed");
        result.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task UpdateApprovalStatus_ShouldReturnSuccessWithNoContent_WhenAllStepsSucceed()
    {
        // Arrange
        var approvalId = Guid.NewGuid();
        var currentUserId = Guid.NewGuid();
        var manager = CreateUser();
        var requester = CreateUser();
        var task = CreateAppTask("Fix the bug");
        var approval = CreateApproval(managerId: currentUserId, type: ApprovalTypeEnum.Task);
        var updatedIncluded = CreateApproval(id: approval.Id, managerId: currentUserId, type: ApprovalTypeEnum.Task,
            task: task, manager: manager, userRequesting: requester);

        _approvalRepositoryMock
            .Setup(r => r.UpdateStatus(approvalId, ApprovalStatusEnum.Approved, It.IsAny<CancellationToken>()))
            .ReturnsAsync(approval);

        _approvalRepositoryMock
            .Setup(r => r.FilterAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Approval, bool>>>(),
                It.IsAny<System.Linq.Expressions.Expression<Func<Approval, object>>[]?>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Approval> { updatedIncluded });

        _activitiesServiceMock
            .Setup(a => a.AddAsync(It.IsAny<ActivityAddDTO>(), currentUserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<ActivityDTO>.Success(_fixture.Create<ActivityDTO>()));

        // Act
        var result = await _tasksApprovalsService.UpdateApprovalStatus(approvalId, ApprovalStatusEnum.Approved, currentUserId);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.StatusCode.Should().Be(HttpStatusCode.NoContent);
        result.Value.Should().NotBeNull();
        result.Value!.Id.Should().Be(updatedIncluded.Id);
        result.Value.RequesterName.Should().Be("You");
        result.Value.Status.Should().Be(ApprovalStatusEnum.Approved);
    }

    [Fact]
    public async Task UpdateApprovalStatus_ShouldSetStatus_BeforeGeneratingActivityContent()
    {
        // Arrange
        var approvalId = Guid.NewGuid();
        var currentUserId = Guid.NewGuid();
        var manager = CreateUser();
        var requester = CreateUser();
        var approval = CreateApproval(managerId: currentUserId, type: ApprovalTypeEnum.Holiday, status: ApprovalStatusEnum.Pending);
        var updatedIncluded = CreateApproval(id: approval.Id, managerId: currentUserId, type: ApprovalTypeEnum.Holiday,
            status: ApprovalStatusEnum.Pending, manager: manager, userRequesting: requester);

        _approvalRepositoryMock
            .Setup(r => r.UpdateStatus(approvalId, ApprovalStatusEnum.Approved, It.IsAny<CancellationToken>()))
            .ReturnsAsync(approval);

        _approvalRepositoryMock
            .Setup(r => r.FilterAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Approval, bool>>>(),
                It.IsAny<System.Linq.Expressions.Expression<Func<Approval, object>>[]?>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Approval> { updatedIncluded });

        ActivityAddDTO? capturedActivity = null;
        _activitiesServiceMock
            .Setup(a => a.AddAsync(It.IsAny<ActivityAddDTO>(), currentUserId, It.IsAny<CancellationToken>()))
            .Callback<ActivityAddDTO, Guid, CancellationToken>((dto, _, _) => capturedActivity = dto)
            .ReturnsAsync(Result<ActivityDTO>.Success(_fixture.Create<ActivityDTO>()));

        // Expected text is generated from the entity AFTER Status is set to Approved
        updatedIncluded.Status = ApprovalStatusEnum.Approved;
        var expectedDescription = ActivityTextGenerator.GetApprovalDescription(updatedIncluded);

        // Act
        await _tasksApprovalsService.UpdateApprovalStatus(approvalId, ApprovalStatusEnum.Approved, currentUserId);

        // Assert
        capturedActivity.Should().NotBeNull();
        capturedActivity!.Description.Should().Be(expectedDescription);
    }

    #endregion
}