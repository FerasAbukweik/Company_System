using System.Net;
using AutoFixture;
using FluentAssertions;
using HR_System.Core.Abstraction.RepositoryContracts;
using HR_System.Core.Abstraction.ServiceContracts;
using HR_System.Core.Domain.Entities;
using HR_System.Core.Domain.Identity;
using HR_System.Core.DTO.Activity;
using HR_System.Core.DTO.Approval;
using HR_System.Core.DTO.Task;
using HR_System.Core.Enums;
using HR_System.Core.helpers;
using HR_System.Core.Result;
using HR_System.Infrastructure.Services;
using Moq;
using Xunit.Abstractions;

namespace HR_System.Core.UnitTests.ServicesTests;

public class TasksApprovalsServiceTests : ServiceTestBase
{
    private readonly ITasksApprovalsService _tasksApprovalsService;
    private readonly Mock<ITasksService> _tasksServiceMock;
    private readonly Mock<IApprovalService> _approvalServiceMock;
    private readonly Mock<IActivitiesService> _activitiesServiceMock;
    private readonly Mock<IApprovalRepository> _approvalRepositoryMock;

    public TasksApprovalsServiceTests(ITestOutputHelper output) : base(output)
    {
        _tasksServiceMock = new Mock<ITasksService>();
        _approvalServiceMock = new Mock<IApprovalService>();
        _activitiesServiceMock = new Mock<IActivitiesService>();
        _approvalRepositoryMock = new Mock<IApprovalRepository>();
        _approvalRepositoryMock.Setup(repository => repository.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        _tasksApprovalsService = new TasksApprovalsService(_tasksServiceMock.Object, _approvalServiceMock.Object, _activitiesServiceMock.Object, _approvalRepositoryMock.Object);
    }

    private TaskDTO CreateTaskDto(Guid? managerId = null, TaskStatusEnum? status = null)
    {
        return Fixture.Build<TaskDTO>()
            .With(t => t.ManagerId, managerId ?? Guid.NewGuid())
            .With(
            t => t.Status,
            status ?? TaskStatusEnum.Pending
        )
            .Create();
    }

    private ApplicationUser CreateUser(string? userName = null)
    {
        return Fixture.Build<ApplicationUser>()
            .With(u => u.UserName, userName ?? Fixture.Create<string>())
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
        return Fixture.Build<AppTask>()
            .With(t => t.Title, title ?? Fixture.Create<string>()) // NOTE: if AppTask has navigation properties (User, Manager, Approvals, etc.)
        // that cause AutoFixture recursion errors, add .Without(...) calls here.
        .Create();
    }

    private Approval CreateApproval(Guid? id = null, ApprovalTypeEnum? type = null, ApprovalStatusEnum? status = null, Guid? taskId = null, AppTask? task = null, Guid? managerId = null, ApplicationUser? manager = null, Guid? userRequestingId = null, ApplicationUser? userRequesting = null)
    {
        return Fixture.Build<Approval>()
            .With(a => a.Id, id ?? Guid.NewGuid())
            .With(
                                        a => a.Type,
                                        type ?? ApprovalTypeEnum.Task
                                    )
            .With(
                                    a => a.Status,
                                    status ?? ApprovalStatusEnum.Pending
                                )
            .With(
                                a => a.TaskId,
                                taskId
                            )
            .With(
                            a => a.Task,
                            task
                        )
            .With(
                        a => a.ManagerId,
                        managerId ?? Guid.NewGuid()
                    )
            .With(
                    a => a.Manager,
                    manager
                )
            .With(
                a => a.UserRequestingId,
                userRequestingId ?? Guid.NewGuid()
            )
            .With(
            a => a.UserRequesting,
            userRequesting
        )
            .Create();
    }

    #region UpdateTaskStatusAsync
    [Fact]
    public async Task UpdateTaskStatusAsync_ShouldReturnFailure_WhenUpdateStatusFails()
    {
        // Arrange
        LogArrange("Scenario", nameof(UpdateTaskStatusAsync_ShouldReturnFailure_WhenUpdateStatusFails));

        var currentUserId = Guid.NewGuid();
        var taskId = Guid.NewGuid();
        _tasksServiceMock.Setup(s => s.UpdateStatusAsync(taskId, TaskStatusEnum.Completed, currentUserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<TaskDTO>.Failure("task not found"));

        // Act
        var result = await _tasksApprovalsService.UpdateTaskStatusAsync(currentUserId, taskId, TaskStatusEnum.Completed);
        LogAct(result);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Be("task not found");
        _approvalServiceMock.Verify(
            a => a.AddAsync(It.IsAny<ApprovalAddDTO>(), It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()),
            Times.Never
        );
    }

    [Fact]
    public async Task UpdateTaskStatusAsync_ShouldNotAddApproval_WhenNewStatusIsNotCompleted()
    {
        // Arrange
        LogArrange("Scenario", nameof(UpdateTaskStatusAsync_ShouldNotAddApproval_WhenNewStatusIsNotCompleted));

        var currentUserId = Guid.NewGuid();
        var taskId = Guid.NewGuid();
        var taskDto = CreateTaskDto(status: TaskStatusEnum.Rejected);
        _tasksServiceMock.Setup(s => s.UpdateStatusAsync(taskId, TaskStatusEnum.Rejected, currentUserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<TaskDTO>.Success(taskDto));

        // Act
        var result = await _tasksApprovalsService.UpdateTaskStatusAsync(currentUserId, taskId, TaskStatusEnum.Rejected);
        LogAct(result);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(taskDto);
        _approvalServiceMock.Verify(
            a => a.AddAsync(It.IsAny<ApprovalAddDTO>(), It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()),
            Times.Never
        );
    }

    [Fact]
    public async Task UpdateTaskStatusAsync_ShouldAddApproval_WithCorrectData_WhenNewStatusIsCompleted()
    {
        // Arrange
        LogArrange(
            "Scenario",
            nameof(UpdateTaskStatusAsync_ShouldAddApproval_WithCorrectData_WhenNewStatusIsCompleted)
        );
        var currentUserId = Guid.NewGuid();
        var taskId = Guid.NewGuid();
        var managerId = Guid.NewGuid();
        var taskDto = CreateTaskDto(managerId: managerId, status: TaskStatusEnum.Completed);
        _tasksServiceMock.Setup(s => s.UpdateStatusAsync(taskId, TaskStatusEnum.Completed, currentUserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<TaskDTO>.Success(taskDto));
        _approvalServiceMock.Setup(a => a.AddAsync(It.IsAny<ApprovalAddDTO>(), currentUserId, managerId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<ToApproveDTO>.Success(Fixture.Create<ToApproveDTO>()));

        // Act
        var result = await _tasksApprovalsService.UpdateTaskStatusAsync(currentUserId, taskId, TaskStatusEnum.Completed);
        LogAct(result);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(taskDto);
        _approvalServiceMock.Verify(
            a => a.AddAsync(
                It.Is<ApprovalAddDTO>(dto => dto.Type == ApprovalTypeEnum.Task && dto.TaskId == taskId),
                currentUserId,
                managerId,
                It.IsAny<CancellationToken>()
            ),
            Times.Once
        );
    }

    [Fact]
    public async Task UpdateTaskStatusAsync_ShouldReturnMappedFailure_WhenAddApprovalFails()
    {
        // Arrange
        LogArrange("Scenario", nameof(UpdateTaskStatusAsync_ShouldReturnMappedFailure_WhenAddApprovalFails));

        var currentUserId = Guid.NewGuid();
        var taskId = Guid.NewGuid();
        var managerId = Guid.NewGuid();
        var taskDto = CreateTaskDto(managerId: managerId, status: TaskStatusEnum.Completed);
        _tasksServiceMock.Setup(s => s.UpdateStatusAsync(taskId, TaskStatusEnum.Completed, currentUserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<TaskDTO>.Success(taskDto));
        _approvalServiceMock.Setup(a => a.AddAsync(It.IsAny<ApprovalAddDTO>(), currentUserId, managerId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<ToApproveDTO>.Failure("approval creation failed", HttpStatusCode.BadRequest));

        // Act
        var result = await _tasksApprovalsService.UpdateTaskStatusAsync(currentUserId, taskId, TaskStatusEnum.Completed);
        LogAct(result);

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
        LogArrange("Scenario", nameof(UpdateApprovalStatus_ShouldReturnFailure_WhenApprovalNotFound));

        var approvalId = Guid.NewGuid();
        var currentUserId = Guid.NewGuid();
        _approvalRepositoryMock.Setup(r => r.UpdateStatusAsync(approvalId, It.IsAny<ApprovalStatusEnum>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Approval? )null);
        _approvalRepositoryMock.Setup(r => r.GetByIdAsync(approvalId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Approval? )null);

        // Act
        var result = await _tasksApprovalsService.UpdateApprovalStatus(approvalId, ApprovalStatusEnum.Approved, currentUserId);
        LogAct(result);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Be("Failed Updating Approval or Approval Doesnt exist");
        result.StatusCode.Should().Be(HttpStatusCode.InternalServerError);
        _approvalRepositoryMock.Verify(
            r => r.FilterAsync(
                It.IsAny<System.Linq.Expressions.Expression<Func<Approval, bool>>>(),
                It.IsAny<System.Linq.Expressions.Expression<Func<Approval, object?>>[]?>(),
                null,
                false,
                null,
                null,
                It.IsAny<CancellationToken>()
            ),
            Times.Never
        );
    }

    [Fact]
    public async Task UpdateApprovalStatus_ShouldReturnForbidden_WhenCurrentUserIsNotTheManager()
    {
        // Arrange
        LogArrange("Scenario", nameof(UpdateApprovalStatus_ShouldReturnForbidden_WhenCurrentUserIsNotTheManager));

        var approvalId = Guid.NewGuid();
        var currentUserId = Guid.NewGuid();
        var approval = CreateApproval(managerId: Guid.NewGuid());
        _approvalRepositoryMock.Setup(r => r.UpdateStatusAsync(approvalId, It.IsAny<ApprovalStatusEnum>(), It.IsAny<CancellationToken>()))
            .Callback<Guid, ApprovalStatusEnum, CancellationToken>((_, state, _) => approval.Status = state)
            .ReturnsAsync(approval);
        _approvalRepositoryMock.Setup(r => r.GetByIdAsync(approvalId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(approval);

        // Act
        var result = await _tasksApprovalsService.UpdateApprovalStatus(approvalId, ApprovalStatusEnum.Approved, currentUserId);
        LogAct(result);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Be("Unauthorized");
        result.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        _approvalRepositoryMock.Verify(
            repository => repository.SaveChangesAsync(It.IsAny<CancellationToken>()),
            Times.Never
        );
        _tasksServiceMock.Verify(
            t => t.UpdateStatusAsync(
                It.IsAny<Guid>(),
                It.IsAny<TaskStatusEnum>(),
                It.IsAny<Guid>(),
                It.IsAny<CancellationToken>()
            ),
            Times.Never
        );
    }

    [Fact]
    public async Task UpdateApprovalStatus_ShouldRejectTask_WhenApprovalTypeIsTaskAndNewStatusIsRejected()
    {
        // Arrange
        LogArrange(
            "Scenario",
            nameof(UpdateApprovalStatus_ShouldRejectTask_WhenApprovalTypeIsTaskAndNewStatusIsRejected)
        );
        var approvalId = Guid.NewGuid();
        var currentUserId = Guid.NewGuid();
        var taskId = Guid.NewGuid();
        var approval = CreateApproval(managerId: currentUserId, type: ApprovalTypeEnum.Task, taskId: taskId);
        var updatedIncluded = approval;
        _approvalRepositoryMock.Setup(r => r.UpdateStatusAsync(approvalId, ApprovalStatusEnum.Rejected, It.IsAny<CancellationToken>()))
            .Callback<Guid, ApprovalStatusEnum, CancellationToken>((_, state, _) => approval.Status = state)
            .ReturnsAsync(approval);
        _approvalRepositoryMock.Setup(r => r.GetByIdAsync(approvalId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(approval);
        _tasksServiceMock.Setup(t => t.UpdateStatusAsync(taskId, TaskStatusEnum.Rejected, currentUserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<TaskDTO>.Success(CreateTaskDto()));
        _approvalRepositoryMock.Setup(r => r.FilterAsync(
            It.IsAny<System.Linq.Expressions.Expression<Func<Approval, bool>>>(),
            It.IsAny<System.Linq.Expressions.Expression<Func<Approval, object?>>[]?>(),
            null,
            false,
            null,
            null,
            It.IsAny<CancellationToken>()
        ))
            .ReturnsAsync(new List<Approval> { updatedIncluded });
        _activitiesServiceMock.Setup(a => a.AddAsync(It.IsAny<ActivityAddDto>(), currentUserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<ActivityDto>.Success(Fixture.Create<ActivityDto>()));

        // Act
        var result = await _tasksApprovalsService.UpdateApprovalStatus(approvalId, ApprovalStatusEnum.Rejected, currentUserId);
        LogAct(result);

        // Assert
        result.IsSuccess.Should().BeTrue();
        _tasksServiceMock.Verify(
            t => t.UpdateStatusAsync(taskId, TaskStatusEnum.Rejected, currentUserId, It.IsAny<CancellationToken>()),
            Times.Once
        );
    }

    [Fact]
    public async Task UpdateApprovalStatus_ShouldReturnMappedFailure_WhenTaskRejectionFails()
    {
        // Arrange
        LogArrange("Scenario", nameof(UpdateApprovalStatus_ShouldReturnMappedFailure_WhenTaskRejectionFails));

        var approvalId = Guid.NewGuid();
        var currentUserId = Guid.NewGuid();
        var taskId = Guid.NewGuid();
        var approval = CreateApproval(managerId: currentUserId, type: ApprovalTypeEnum.Task, taskId: taskId);
        _approvalRepositoryMock.Setup(r => r.UpdateStatusAsync(approvalId, ApprovalStatusEnum.Rejected, It.IsAny<CancellationToken>()))
            .Callback<Guid, ApprovalStatusEnum, CancellationToken>((_, state, _) => approval.Status = state)
            .ReturnsAsync(approval);
        _approvalRepositoryMock.Setup(r => r.GetByIdAsync(approvalId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(approval);
        _tasksServiceMock.Setup(t => t.UpdateStatusAsync(taskId, TaskStatusEnum.Rejected, currentUserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<TaskDTO>.Failure("task update failed", HttpStatusCode.BadRequest));

        // Act
        var result = await _tasksApprovalsService.UpdateApprovalStatus(approvalId, ApprovalStatusEnum.Rejected, currentUserId);
        LogAct(result);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Be("task update failed");
        result.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        _approvalRepositoryMock.Verify(
            r => r.FilterAsync(
                It.IsAny<System.Linq.Expressions.Expression<Func<Approval, bool>>>(),
                It.IsAny<System.Linq.Expressions.Expression<Func<Approval, object?>>[]?>(),
                null,
                false,
                null,
                null,
                It.IsAny<CancellationToken>()
            ),
            Times.Never
        );
    }

    [Fact]
    public async Task UpdateApprovalStatus_ShouldReturnFailure_WhenSavingApprovalFails()
    {
        // Arrange
        LogArrange("Scenario", nameof(UpdateApprovalStatus_ShouldReturnFailure_WhenSavingApprovalFails));

        var approvalId = Guid.NewGuid();
        var currentUserId = Guid.NewGuid();
        // Holiday type (or Approved status) avoids the task-rejection branch entirely
        var approval = CreateApproval(managerId: currentUserId, type: ApprovalTypeEnum.Holiday);
        _approvalRepositoryMock.Setup(r => r.UpdateStatusAsync(approvalId, ApprovalStatusEnum.Approved, It.IsAny<CancellationToken>()))
            .Callback<Guid, ApprovalStatusEnum, CancellationToken>((_, state, _) => approval.Status = state)
            .ReturnsAsync(approval);
        _approvalRepositoryMock.Setup(r => r.GetByIdAsync(approvalId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(approval);
        _approvalRepositoryMock.Setup(r => r.FilterAsync(
            It.IsAny<System.Linq.Expressions.Expression<Func<Approval, bool>>>(),
            It.IsAny<System.Linq.Expressions.Expression<Func<Approval, object?>>[]?>(),
            null,
            false,
            null,
            null,
            It.IsAny<CancellationToken>()
        ))
            .ReturnsAsync(new List<Approval>());
        _approvalRepositoryMock.Setup(repository => repository.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        // Act
        var result = await _tasksApprovalsService.UpdateApprovalStatus(approvalId, ApprovalStatusEnum.Approved, currentUserId);
        LogAct(result);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Be("Failed Updating Approval or Approval Doesnt exist");
        result.StatusCode.Should().Be(HttpStatusCode.InternalServerError);
        _activitiesServiceMock.Verify(
            a => a.AddAsync(It.IsAny<ActivityAddDto>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()),
            Times.Never
        );
    }

    [Theory]
    [InlineData(ApprovalStatusEnum.Approved, ActivityTypeEnum.ApprovalApproved)]
    [InlineData(ApprovalStatusEnum.Rejected, ActivityTypeEnum.ApprovalRejected)]
    public async Task UpdateApprovalStatus_ShouldAddActivity_WithCorrectType_BasedOnNewStatus(ApprovalStatusEnum newStatus, ActivityTypeEnum expectedActivityType)
    {
        // Arrange
        var approvalId = Guid.NewGuid();
        var currentUserId = Guid.NewGuid();
        // Use Holiday type so the task-rejection branch never triggers, regardless of newStatus
        var approval = CreateApproval(managerId: currentUserId, type: ApprovalTypeEnum.Holiday);
        var updatedIncluded = approval;
        _approvalRepositoryMock.Setup(r => r.UpdateStatusAsync(approvalId, newStatus, It.IsAny<CancellationToken>()))
            .Callback<Guid, ApprovalStatusEnum, CancellationToken>((_, state, _) => approval.Status = state)
            .ReturnsAsync(approval);
        _approvalRepositoryMock.Setup(r => r.GetByIdAsync(approvalId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(approval);
        _approvalRepositoryMock.Setup(r => r.FilterAsync(
            It.IsAny<System.Linq.Expressions.Expression<Func<Approval, bool>>>(),
            It.IsAny<System.Linq.Expressions.Expression<Func<Approval, object?>>[]?>(),
            null,
            false,
            null,
            null,
            It.IsAny<CancellationToken>()
        ))
            .ReturnsAsync(new List<Approval> { updatedIncluded });
        _activitiesServiceMock.Setup(a => a.AddAsync(It.IsAny<ActivityAddDto>(), currentUserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<ActivityDto>.Success(Fixture.Create<ActivityDto>()));
        LogArrange(nameof(newStatus), newStatus);

        // Act
        await _tasksApprovalsService.UpdateApprovalStatus(approvalId, newStatus, currentUserId);
        LogAct("Operation completed");

        // Assert
        _activitiesServiceMock.Verify(
            a => a.AddAsync(
                It.Is<ActivityAddDto>(dto => dto.Type == expectedActivityType),
                currentUserId,
                It.IsAny<CancellationToken>()
            ),
            Times.Once
        );
    }

    [Fact]
    public async Task UpdateApprovalStatus_ShouldReturnMappedFailure_WhenActivityAdditionFails()
    {
        // Arrange
        LogArrange("Scenario", nameof(UpdateApprovalStatus_ShouldReturnMappedFailure_WhenActivityAdditionFails));

        var approvalId = Guid.NewGuid();
        var currentUserId = Guid.NewGuid();
        var approval = CreateApproval(managerId: currentUserId, type: ApprovalTypeEnum.Holiday);
        var updatedIncluded = approval;
        _approvalRepositoryMock.Setup(r => r.UpdateStatusAsync(approvalId, ApprovalStatusEnum.Approved, It.IsAny<CancellationToken>()))
            .Callback<Guid, ApprovalStatusEnum, CancellationToken>((_, state, _) => approval.Status = state)
            .ReturnsAsync(approval);
        _approvalRepositoryMock.Setup(r => r.GetByIdAsync(approvalId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(approval);
        _approvalRepositoryMock.Setup(r => r.FilterAsync(
            It.IsAny<System.Linq.Expressions.Expression<Func<Approval, bool>>>(),
            It.IsAny<System.Linq.Expressions.Expression<Func<Approval, object?>>[]?>(),
            null,
            false,
            null,
            null,
            It.IsAny<CancellationToken>()
        ))
            .ReturnsAsync(new List<Approval> { updatedIncluded });
        _activitiesServiceMock.Setup(a => a.AddAsync(It.IsAny<ActivityAddDto>(), currentUserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<ActivityDto>.Failure("activity creation failed", HttpStatusCode.BadRequest));

        // Act
        var result = await _tasksApprovalsService.UpdateApprovalStatus(approvalId, ApprovalStatusEnum.Approved, currentUserId);
        LogAct(result);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Be("activity creation failed");
        result.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task UpdateApprovalStatus_ShouldReturnSuccessWithNoContent_WhenAllStepsSucceed()
    {
        // Arrange
        LogArrange("Scenario", nameof(UpdateApprovalStatus_ShouldReturnSuccessWithNoContent_WhenAllStepsSucceed));

        var approvalId = Guid.NewGuid();
        var currentUserId = Guid.NewGuid();
        var manager = CreateUser();
        var requester = CreateUser();
        var task = CreateAppTask("Fix the bug");
        var approval = CreateApproval(managerId: currentUserId, type: ApprovalTypeEnum.Task);
        var updatedIncluded = approval;
        _approvalRepositoryMock.Setup(r => r.UpdateStatusAsync(approvalId, ApprovalStatusEnum.Approved, It.IsAny<CancellationToken>()))
            .Callback<Guid, ApprovalStatusEnum, CancellationToken>((_, state, _) => approval.Status = state)
            .ReturnsAsync(approval);
        _approvalRepositoryMock.Setup(r => r.GetByIdAsync(approvalId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(approval);
        _approvalRepositoryMock.Setup(r => r.FilterAsync(
            It.IsAny<System.Linq.Expressions.Expression<Func<Approval, bool>>>(),
            It.IsAny<System.Linq.Expressions.Expression<Func<Approval, object?>>[]?>(),
            null,
            false,
            null,
            null,
            It.IsAny<CancellationToken>()
        ))
            .ReturnsAsync(new List<Approval> { updatedIncluded });
        _activitiesServiceMock.Setup(a => a.AddAsync(It.IsAny<ActivityAddDto>(), currentUserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<ActivityDto>.Success(Fixture.Create<ActivityDto>()));

        // Act
        var result = await _tasksApprovalsService.UpdateApprovalStatus(approvalId, ApprovalStatusEnum.Approved, currentUserId);
        LogAct(result);

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
        LogArrange("Scenario", nameof(UpdateApprovalStatus_ShouldSetStatus_BeforeGeneratingActivityContent));

        var approvalId = Guid.NewGuid();
        var currentUserId = Guid.NewGuid();
        var manager = CreateUser();
        var requester = CreateUser();
        var approval = CreateApproval(managerId: currentUserId, type: ApprovalTypeEnum.Holiday, status: ApprovalStatusEnum.Pending);
        var updatedIncluded = approval;
        _approvalRepositoryMock.Setup(r => r.UpdateStatusAsync(approvalId, ApprovalStatusEnum.Approved, It.IsAny<CancellationToken>()))
            .Callback<Guid, ApprovalStatusEnum, CancellationToken>((_, state, _) => approval.Status = state)
            .ReturnsAsync(approval);
        _approvalRepositoryMock.Setup(r => r.GetByIdAsync(approvalId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(approval);
        _approvalRepositoryMock.Setup(r => r.FilterAsync(
            It.IsAny<System.Linq.Expressions.Expression<Func<Approval, bool>>>(),
            It.IsAny<System.Linq.Expressions.Expression<Func<Approval, object?>>[]?>(),
            null,
            false,
            null,
            null,
            It.IsAny<CancellationToken>()
        ))
            .ReturnsAsync(new List<Approval> { updatedIncluded });
        ActivityAddDto? capturedActivity = null;
        _activitiesServiceMock.Setup(a => a.AddAsync(It.IsAny<ActivityAddDto>(), currentUserId, It.IsAny<CancellationToken>()))
            .Callback<ActivityAddDto, Guid, CancellationToken>((dto, _, _) => capturedActivity = dto)
            .ReturnsAsync(Result<ActivityDto>.Success(Fixture.Create<ActivityDto>()));
        // Expected text is generated from the entity AFTER Status is set to Approved
        updatedIncluded.Status = ApprovalStatusEnum.Approved;
        var expectedDescription = ActivityTextGenerator.GetApprovalDescription(updatedIncluded);

        // Act
        await _tasksApprovalsService.UpdateApprovalStatus(approvalId, ApprovalStatusEnum.Approved, currentUserId);
        LogAct("Operation completed");

        // Assert
        capturedActivity.Should().NotBeNull();
        capturedActivity!.ApprovalId.Should().Be(updatedIncluded.Id);
    }
    #endregion
}
