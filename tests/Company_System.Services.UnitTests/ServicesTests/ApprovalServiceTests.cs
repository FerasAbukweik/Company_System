using System.Net;
using AutoFixture;
using FluentAssertions;
using HR_System.Core.Domain.Entities;
using HR_System.Core.Domain.Identity;
using HR_System.Core.DTO.Approval;
using HR_System.Core.Enums;
using HR_System.Core.Interfaces.RepositoryContracts;
using HR_System.Infrastructure.Services;
using Microsoft.AspNetCore.Identity;
using Moq;
using Xunit.Abstractions;

namespace HR_System.Core.UnitTests.ServicesTests;

public class ApprovalServiceTests
{
    private readonly ITestOutputHelper _output;
    private readonly IFixture _fixture;
    private readonly Mock<IApprovalRepository> _approvalRepositoryMock;
    private readonly Mock<ITasksRepository> _tasksRepositoryMock;
    private readonly Mock<UserManager<ApplicationUser>> _userManagerMock;
    private readonly ApprovalService _approvalService;

    public ApprovalServiceTests(ITestOutputHelper output)
    {
        _output = output;

        _fixture = new Fixture();
        _fixture.Behaviors.OfType<ThrowingRecursionBehavior>().ToList()
            .ForEach(b => _fixture.Behaviors.Remove(b));
        _fixture.Behaviors.Add(new OmitOnRecursionBehavior());

        _approvalRepositoryMock = new Mock<IApprovalRepository>();
        _tasksRepositoryMock = new Mock<ITasksRepository>();

        // UserManager requires special constructor mocking
        _userManagerMock = new Mock<UserManager<ApplicationUser>>(
            Mock.Of<IUserStore<ApplicationUser>>(),
            null!, null!, null!, null!, null!, null!, null!, null!);

        _approvalService = new ApprovalService(
            _approvalRepositoryMock.Object,
            _userManagerMock.Object,
            _tasksRepositoryMock.Object);
    }

    #region GetManagerToApproveAsync

    [Fact]
    public async Task GetManagerToApproveAsync_WithApprovals_ShouldReturnMappedDTOs()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var approvals = CreateMany(3);

        _approvalRepositoryMock
            .Setup(r => r.GetManagerToApprove(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(approvals);

        _output.WriteLine($"UserId        : {userId}");
        _output.WriteLine($"Expected Count: {approvals.Count}");
        approvals.ForEach(a => _output.WriteLine($"  Expected: {a.Id} | {a.Type}"));

        // Act
        var actual = await _approvalService.GetManagerToApproveAsync(userId);
        _output.WriteLine($"Actual Count: {actual.Value?.Count ?? -1}");
        actual.Value?.ToList().ForEach(a => _output.WriteLine($"  Actual: {a.Id} | {a.Type}"));

        // Assert
        actual.Should().NotBeNull();
        actual.IsSuccess.Should().BeTrue();
        actual.Value.Should().HaveCount(approvals.Count);
        actual.Value.Should().BeAssignableTo<IReadOnlyList<ApprovalDTO>>();

        _approvalRepositoryMock.Verify(r =>
            r.GetManagerToApprove(userId, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetManagerToApproveAsync_NoApprovals_ShouldReturnEmpty()
    {
        // Arrange
        var userId = Guid.NewGuid();

        _approvalRepositoryMock
            .Setup(r => r.GetManagerToApprove(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);

        _output.WriteLine($"UserId        : {userId}");
        _output.WriteLine("Expected Count: 0");

        // Act
        var actual = await _approvalService.GetManagerToApproveAsync(userId);
        _output.WriteLine($"Actual Count: {actual.Value?.Count ?? -1}");

        // Assert
        actual.Should().NotBeNull();
        actual.IsSuccess.Should().BeTrue();
        actual.Value.Should().BeEmpty();

        _approvalRepositoryMock.Verify(r =>
            r.GetManagerToApprove(userId, It.IsAny<CancellationToken>()), Times.Once);
    }

    #endregion

    #region AddAsync — Holiday Type

    [Fact]
    public async Task AddAsync_HolidayType_ValidUser_ShouldSucceed()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var user = CreateUser(userId);
        var toAdd = _fixture.Build<ApprovalAddDTO>()
            .With(a => a.Type, ApprovalTypeEnum.Holiday)
            .Create();

        _userManagerMock
            .Setup(m => m.FindByIdAsync(userId.ToString()))
            .ReturnsAsync(user);
        _approvalRepositoryMock
            .Setup(r => r.Add(It.IsAny<Approval>()));
        _approvalRepositoryMock
            .Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        _output.WriteLine($"UserId  : {userId}");
        _output.WriteLine($"UserName: {user.UserName}");
        _output.WriteLine($"Type    : {toAdd.Type}");

        // Act
        var actual = await _approvalService.AddAsync(toAdd, userId);
        _output.WriteLine($"IsSuccess       : {actual.IsSuccess}");
        _output.WriteLine($"Actual Type     : {actual.Value?.Type}");
        _output.WriteLine($"Actual UserReqId: {actual.Value?.UserRequestingId}");

        // Assert
        actual.Should().NotBeNull();
        actual.IsSuccess.Should().BeTrue();
        actual.Value.Should().NotBeNull();
        actual.Value!.Type.Should().Be(ApprovalTypeEnum.Holiday);
        actual.Value!.UserRequestingId.Should().Be(userId);

        _userManagerMock.Verify(m => m.FindByIdAsync(userId.ToString()), Times.Once);
        _approvalRepositoryMock.Verify(r => r.Add(It.IsAny<Approval>()), Times.Once);
        _approvalRepositoryMock.Verify(r =>
            r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task AddAsync_HolidayType_UserNotFound_ShouldReturnFailure()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var toAdd = _fixture.Build<ApprovalAddDTO>()
            .With(a => a.Type, ApprovalTypeEnum.Holiday)
            .Create();

        _userManagerMock
            .Setup(m => m.FindByIdAsync(userId.ToString()))
            .ReturnsAsync(null as ApplicationUser); // user not found

        _output.WriteLine($"UserId: {userId}");
        _output.WriteLine("User not found — expecting Unauthorized failure");

        // Act
        var actual = await _approvalService.AddAsync(toAdd, userId);
        _output.WriteLine($"IsSuccess    : {actual.IsSuccess}");
        _output.WriteLine($"StatusCode   : {actual.StatusCode}");
        _output.WriteLine($"ErrorMessage : {actual.ErrorMessage}");

        // Assert
        actual.Should().NotBeNull();
        actual.IsSuccess.Should().BeFalse();
        actual.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        actual.ErrorMessage.Should().Be("user not found");

        // Add and SaveChanges should never be called
        _approvalRepositoryMock.Verify(r => r.Add(It.IsAny<Approval>()), Times.Never);
        _approvalRepositoryMock.Verify(r =>
            r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    #endregion

    #region AddAsync — Task Type

    [Fact]
    public async Task AddAsync_TaskType_ValidTask_ShouldSucceed()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var task = CreateTask();
        var toAdd = _fixture.Build<ApprovalAddDTO>()
            .With(a => a.Type, ApprovalTypeEnum.Task)
            .With(a => a.TaskId, task.Id)
            .Create();

        _tasksRepositoryMock
            .Setup(r => r.GetTaskAsync(task.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(task);
        _approvalRepositoryMock
            .Setup(r => r.Add(It.IsAny<Approval>()));
        _approvalRepositoryMock
            .Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        _output.WriteLine($"UserId : {userId}");
        _output.WriteLine($"TaskId : {task.Id}");
        _output.WriteLine($"Type   : {toAdd.Type}");

        // Act
        var actual = await _approvalService.AddAsync(toAdd, userId);
        _output.WriteLine($"IsSuccess       : {actual.IsSuccess}");
        _output.WriteLine($"Actual Type     : {actual.Value?.Type}");
        _output.WriteLine($"Actual TaskId   : {actual.Value?.TaskId}");

        // Assert
        actual.Should().NotBeNull();
        actual.IsSuccess.Should().BeTrue();
        actual.Value.Should().NotBeNull();
        actual.Value!.Type.Should().Be(ApprovalTypeEnum.Task);
        actual.Value!.TaskId.Should().Be(task.Id);

        _tasksRepositoryMock.Verify(r =>
            r.GetTaskAsync(task.Id, It.IsAny<CancellationToken>()), Times.Once);
        _approvalRepositoryMock.Verify(r => r.Add(It.IsAny<Approval>()), Times.Once);
        _approvalRepositoryMock.Verify(r =>
            r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task AddAsync_TaskType_NullTaskId_ShouldReturnFailure()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var toAdd = _fixture.Build<ApprovalAddDTO>()
            .With(a => a.Type, ApprovalTypeEnum.Task)
            .With(a => a.TaskId, (Guid?)null) // null taskId
            .Create();

        _output.WriteLine($"UserId: {userId}");
        _output.WriteLine("TaskId is null — expecting BadRequest failure");

        // Act
        var actual = await _approvalService.AddAsync(toAdd, userId);
        _output.WriteLine($"IsSuccess    : {actual.IsSuccess}");
        _output.WriteLine($"StatusCode   : {actual.StatusCode}");
        _output.WriteLine($"ErrorMessage : {actual.ErrorMessage}");

        // Assert
        actual.Should().NotBeNull();
        actual.IsSuccess.Should().BeFalse();
        actual.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        actual.ErrorMessage.Should().Be("taskId is required");

        _approvalRepositoryMock.Verify(r => r.Add(It.IsAny<Approval>()), Times.Never);
        _approvalRepositoryMock.Verify(r =>
            r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task AddAsync_TaskType_TaskNotFound_ShouldReturnFailure()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var taskId = Guid.NewGuid();
        var toAdd = _fixture.Build<ApprovalAddDTO>()
            .With(a => a.Type, ApprovalTypeEnum.Task)
            .With(a => a.TaskId, taskId)
            .Create();

        _tasksRepositoryMock
            .Setup(r => r.GetTaskAsync(taskId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(null as AppTask); // task not found

        _output.WriteLine($"UserId: {userId}");
        _output.WriteLine($"TaskId: {taskId}");
        _output.WriteLine("Task not found — expecting NotFound failure");

        // Act
        var actual = await _approvalService.AddAsync(toAdd, userId);
        _output.WriteLine($"IsSuccess    : {actual.IsSuccess}");
        _output.WriteLine($"StatusCode   : {actual.StatusCode}");
        _output.WriteLine($"ErrorMessage : {actual.ErrorMessage}");

        // Assert
        actual.Should().NotBeNull();
        actual.IsSuccess.Should().BeFalse();
        actual.StatusCode.Should().Be(HttpStatusCode.NotFound);
        actual.ErrorMessage.Should().Be("task not found");

        _approvalRepositoryMock.Verify(r => r.Add(It.IsAny<Approval>()), Times.Never);
        _approvalRepositoryMock.Verify(r =>
            r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task AddAsync_SaveChangesFails_ShouldReturnFailure()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var user = CreateUser(userId);
        var toAdd = _fixture.Build<ApprovalAddDTO>()
            .With(a => a.Type, ApprovalTypeEnum.Holiday)
            .Create();

        _userManagerMock
            .Setup(m => m.FindByIdAsync(userId.ToString()))
            .ReturnsAsync(user);
        _approvalRepositoryMock
            .Setup(r => r.Add(It.IsAny<Approval>()));
        _approvalRepositoryMock
            .Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        _output.WriteLine($"UserId: {userId}");
        _output.WriteLine("SaveChanges returns false — expecting failure");

        // Act
        var actual = await _approvalService.AddAsync(toAdd, userId);
        _output.WriteLine($"IsSuccess    : {actual.IsSuccess}");
        _output.WriteLine($"ErrorMessage : {actual.ErrorMessage}");

        // Assert
        actual.Should().NotBeNull();
        actual.IsSuccess.Should().BeFalse();
        actual.ErrorMessage.Should().Be("Failed saving Data to DB");

        _approvalRepositoryMock.Verify(r => r.Add(It.IsAny<Approval>()), Times.Once);
        _approvalRepositoryMock.Verify(r =>
            r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    #endregion

    #region UpdateStatus

    [Fact]
    public async Task UpdateStatus_ValidData_ShouldSucceed()
    {
        // Arrange
        var currentUserId = Guid.NewGuid();
        var approval = CreateApproval(managerId: currentUserId);
        var newStatus = ApprovalStatusEnum.Approved;

        _approvalRepositoryMock
            .Setup(r => r.UpdateStatus(approval.Id, newStatus, It.IsAny<CancellationToken>()))
            .ReturnsAsync(approval);
        _approvalRepositoryMock
            .Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        _output.WriteLine($"ApprovalId   : {approval.Id}");
        _output.WriteLine($"ManagerId    : {approval.ManagerId}");
        _output.WriteLine($"CurrentUserId: {currentUserId}");
        _output.WriteLine($"New Status   : {newStatus}");

        // Act
        var actual = await _approvalService.UpdateStatus(approval.Id, newStatus, currentUserId);
        _output.WriteLine($"IsSuccess: {actual.IsSuccess}");
        _output.WriteLine($"Actual Id: {actual.Value?.Id}");

        // Assert
        actual.Should().NotBeNull();
        actual.IsSuccess.Should().BeTrue();
        actual.Value.Should().NotBeNull();
        actual.Value!.Id.Should().Be(approval.Id);

        _approvalRepositoryMock.Verify(r =>
            r.UpdateStatus(approval.Id, newStatus, It.IsAny<CancellationToken>()), Times.Once);
        _approvalRepositoryMock.Verify(r =>
            r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task UpdateStatus_ApprovalNotFound_ShouldReturnFailure()
    {
        // Arrange
        var approvalId = Guid.NewGuid();
        var currentUserId = Guid.NewGuid();
        var newStatus = ApprovalStatusEnum.Approved;

        _approvalRepositoryMock
            .Setup(r => r.UpdateStatus(approvalId, newStatus, It.IsAny<CancellationToken>()))
            .ReturnsAsync(null as Approval);

        _output.WriteLine($"ApprovalId: {approvalId}");
        _output.WriteLine("UpdateStatus returns null — expecting failure");

        // Act
        var actual = await _approvalService.UpdateStatus(approvalId, newStatus, currentUserId);
        _output.WriteLine($"IsSuccess    : {actual.IsSuccess}");
        _output.WriteLine($"ErrorMessage : {actual.ErrorMessage}");

        // Assert
        actual.Should().NotBeNull();
        actual.IsSuccess.Should().BeFalse();
        actual.ErrorMessage.Should().Be("Failed Updating Approval or Approval Doesnt exist");

        _approvalRepositoryMock.Verify(r =>
            r.UpdateStatus(approvalId, newStatus, It.IsAny<CancellationToken>()), Times.Once);
        _approvalRepositoryMock.Verify(r =>
            r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task UpdateStatus_WrongManager_ShouldReturnUnauthorized()
    {
        // Arrange
        var currentUserId = Guid.NewGuid();
        var approval = CreateApproval(managerId: Guid.NewGuid()); // different manager
        var newStatus = ApprovalStatusEnum.Approved;

        _approvalRepositoryMock
            .Setup(r => r.UpdateStatus(approval.Id, newStatus, It.IsAny<CancellationToken>()))
            .ReturnsAsync(approval);

        _output.WriteLine($"ApprovalId      : {approval.Id}");
        _output.WriteLine($"Approval Manager: {approval.ManagerId}");
        _output.WriteLine($"Current User    : {currentUserId}");
        _output.WriteLine("Manager mismatch — expecting Unauthorized");

        // Act
        var actual = await _approvalService.UpdateStatus(approval.Id, newStatus, currentUserId);
        _output.WriteLine($"IsSuccess    : {actual.IsSuccess}");
        _output.WriteLine($"StatusCode   : {actual.StatusCode}");
        _output.WriteLine($"ErrorMessage : {actual.ErrorMessage}");

        // Assert
        actual.Should().NotBeNull();
        actual.IsSuccess.Should().BeFalse();
        actual.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        actual.ErrorMessage.Should().Be("Unauthorized");

        _approvalRepositoryMock.Verify(r =>
            r.UpdateStatus(approval.Id, newStatus, It.IsAny<CancellationToken>()), Times.Once);
        _approvalRepositoryMock.Verify(r =>
            r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task UpdateStatus_SaveChangesFails_ShouldReturnFailure()
    {
        // Arrange
        var currentUserId = Guid.NewGuid();
        var approval = CreateApproval(managerId: currentUserId);
        var newStatus = ApprovalStatusEnum.Rejected;

        _approvalRepositoryMock
            .Setup(r => r.UpdateStatus(approval.Id, newStatus, It.IsAny<CancellationToken>()))
            .ReturnsAsync(approval);
        _approvalRepositoryMock
            .Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        _output.WriteLine($"ApprovalId: {approval.Id}");
        _output.WriteLine("SaveChanges returns false — expecting failure");

        // Act
        var actual = await _approvalService.UpdateStatus(approval.Id, newStatus, currentUserId);
        _output.WriteLine($"IsSuccess    : {actual.IsSuccess}");
        _output.WriteLine($"ErrorMessage : {actual.ErrorMessage}");

        // Assert
        actual.Should().NotBeNull();
        actual.IsSuccess.Should().BeFalse();
        actual.ErrorMessage.Should().Be("Failed saving Data to DB");

        _approvalRepositoryMock.Verify(r =>
            r.UpdateStatus(approval.Id, newStatus, It.IsAny<CancellationToken>()), Times.Once);
        _approvalRepositoryMock.Verify(r =>
            r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    #endregion

    #region Helpers

    private Approval CreateApproval(Guid? managerId = null) =>
        _fixture.Build<Approval>()
            .With(a => a.ManagerId, managerId ?? Guid.NewGuid())
            .Create();

    private List<Approval> CreateMany(int count, Guid? managerId = null) =>
        Enumerable.Range(0, count)
            .Select(_ => CreateApproval(managerId))
            .ToList();

    private ApplicationUser CreateUser(Guid? id = null) =>
        _fixture.Build<ApplicationUser>()
            .With(u => u.Id, id ?? Guid.NewGuid())
            .Create();

    private AppTask CreateTask() =>
        _fixture.Build<AppTask>()
            .Create();

    #endregion
}