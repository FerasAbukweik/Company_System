using AutoFixture;
using FluentAssertions;
using HR_System.Core.Abstraction.RepositoryContracts;
using HR_System.Core.Abstraction.ServiceContracts;
using HR_System.Core.Domain.Entities;
using HR_System.Core.DTO.Activity;
using HR_System.Core.DTO.Approval;
using HR_System.Core.DTO.LazyLoading;
using HR_System.Core.Enums;
using HR_System.Core.Result;
using HR_System.Infrastructure.Services;
using Moq;
using Xunit.Abstractions;

namespace HR_System.Core.UnitTests.ServicesTests;

public class ApprovalServiceTests : ServiceTestBase
{
    private readonly IApprovalService _approvalService;
    private readonly Mock<IApprovalRepository> _approvalRepositoryMock;
    private readonly Mock<IActivitiesService> _activitiesServiceMock;
    private readonly Mock<IOrganizationHierarchyRepository> _hierarchyRepositoryMock;

    public ApprovalServiceTests(ITestOutputHelper output) : base(output)
    {
        _approvalRepositoryMock = new Mock<IApprovalRepository>();
        _activitiesServiceMock = new Mock<IActivitiesService>();
        _hierarchyRepositoryMock = new Mock<IOrganizationHierarchyRepository>();
        _approvalService = new ApprovalService(_approvalRepositoryMock.Object, _activitiesServiceMock.Object, _hierarchyRepositoryMock.Object);
    }

    private Approval CreateApproval(Guid? managerId = null, Guid? userRequestingId = null, ApprovalStatusEnum? status = null, ApprovalTypeEnum? type = null)
    {
        return Fixture.Build<Approval>()
            .With(a => a.ManagerId, managerId ?? Guid.NewGuid())
            .With(
                    a => a.UserRequestingId,
                    userRequestingId ?? Guid.NewGuid()
                )
            .With(
                a => a.Status,
                status ?? ApprovalStatusEnum.Pending
            )
            .With(
            a => a.Type,
            type ?? ApprovalTypeEnum.Holiday
        )
            .Without(a => a.Task)
            .Without(a => a.UserRequesting)
            .Without(a => a.Manager)
            .Create();
    }

    private OrganizationHierarchy CreateHierarchyNode(Guid userId, OrganizationHierarchy? parent = null)
    {
        return Fixture.Build<OrganizationHierarchy>()
            .With(o => o.UserId, userId)
            .With(o => o.Parent, parent)
            .With(
            o => o.ParentId,
            parent?.Id
        )
            .Without(o => o.User)
            .Without(o => o.Children)
            .Create();
    }

    #region GetNeedsApprovalAsync
    [Fact]
    public async Task GetNeedsApprovalAsync_ShouldReturnMappedDtos_WhenRepositoryReturnsApprovals()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var lazyData = new LazyDTO
        {
            Taken = 0,
            SectionSize = 10
        };
        var approval = CreateApproval(managerId: userId);
        _approvalRepositoryMock.Setup(r => r.FilterAsync(
            It.IsAny<System.Linq.Expressions.Expression<Func<Approval, bool>>>(),
            It.IsAny<System.Linq.Expressions.Expression<Func<Approval, object?>>[]?>(),
            It.IsAny<System.Linq.Expressions.Expression<Func<Approval, object?>>?>(),
            true,
            lazyData.Taken,
            lazyData.SectionSize,
            It.IsAny<CancellationToken>()
        ))
            .ReturnsAsync(new List<Approval> { approval });
        LogArrange(nameof(userId), userId);

        // Act
        var result = await _approvalService.GetNeedsApprovalAsync(lazyData, userId);
        LogAct(result);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().ContainSingle(dto => dto.Id == approval.Id);
    }

    [Fact]
    public async Task GetNeedsApprovalAsync_ShouldReturnEmptyList_WhenNoApprovalsMatch()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var lazyData = new LazyDTO
        {
            Taken = 0,
            SectionSize = 10
        };
        _approvalRepositoryMock.Setup(r => r.FilterAsync(
            It.IsAny<System.Linq.Expressions.Expression<Func<Approval, bool>>>(),
            It.IsAny<System.Linq.Expressions.Expression<Func<Approval, object?>>[]?>(),
            It.IsAny<System.Linq.Expressions.Expression<Func<Approval, object?>>?>(),
            true,
            lazyData.Taken,
            lazyData.SectionSize,
            It.IsAny<CancellationToken>()
        ))
            .ReturnsAsync(new List<Approval>());
        LogArrange(nameof(userId), userId);

        // Act
        var result = await _approvalService.GetNeedsApprovalAsync(lazyData, userId);
        LogAct(result);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeEmpty();
    }

    #endregion
    #region GetRequested
    [Fact]
    public async Task GetRequested_ShouldReturnMappedDtos_WhenRepositoryReturnsApprovals()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var lazyData = new LazyDTO
        {
            Taken = 0,
            SectionSize = 10
        };
        var approval = CreateApproval(userRequestingId: userId);
        _approvalRepositoryMock.Setup(r => r.FilterAsync(
            It.IsAny<System.Linq.Expressions.Expression<Func<Approval, bool>>>(),
            It.IsAny<System.Linq.Expressions.Expression<Func<Approval, object?>>[]?>(),
            It.IsAny<System.Linq.Expressions.Expression<Func<Approval, object?>>?>(),
            true,
            lazyData.Taken,
            lazyData.SectionSize,
            It.IsAny<CancellationToken>()
        ))
            .ReturnsAsync(new List<Approval> { approval });
        LogArrange(nameof(userId), userId);

        // Act
        var result = await _approvalService.GetRequested(lazyData, userId);
        LogAct(result);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().ContainSingle(dto => dto.Id == approval.Id);
    }

    [Fact]
    public async Task GetRequested_ShouldReturnEmptyList_WhenNoApprovalsMatch()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var lazyData = new LazyDTO
        {
            Taken = 0,
            SectionSize = 10
        };
        _approvalRepositoryMock.Setup(r => r.FilterAsync(
            It.IsAny<System.Linq.Expressions.Expression<Func<Approval, bool>>>(),
            It.IsAny<System.Linq.Expressions.Expression<Func<Approval, object?>>[]?>(),
            It.IsAny<System.Linq.Expressions.Expression<Func<Approval, object?>>?>(),
            true,
            lazyData.Taken,
            lazyData.SectionSize,
            It.IsAny<CancellationToken>()
        ))
            .ReturnsAsync(new List<Approval>());
        LogArrange(nameof(userId), userId);

        // Act
        var result = await _approvalService.GetRequested(lazyData, userId);
        LogAct(result);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeEmpty();
    }

    #endregion
    #region UpdateStatus
    [Fact]
    public async Task UpdateStatus_ShouldReturnFailure_WhenApprovalNotFound()
    {
        // Arrange
        LogArrange("Scenario", nameof(UpdateStatus_ShouldReturnFailure_WhenApprovalNotFound));

        var approvalId = Guid.NewGuid();
        _approvalRepositoryMock.Setup(r => r.UpdateStatusAsync(approvalId, ApprovalStatusEnum.Approved, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Approval? )null);
        _approvalRepositoryMock.Setup(r => r.GetByIdAsync(approvalId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Approval? )null);

        // Act
        var result = await _approvalService.UpdateStatus(approvalId, ApprovalStatusEnum.Approved, Guid.NewGuid());
        LogAct(result);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Be("Failed to update approval or approval doesnt exist");
    }

    [Fact]
    public async Task UpdateStatus_ShouldReturnForbidden_WhenCurrentUserIsNotTheManager()
    {
        // Arrange
        LogArrange("Scenario", nameof(UpdateStatus_ShouldReturnForbidden_WhenCurrentUserIsNotTheManager));

        var approvalId = Guid.NewGuid();
        var actualManagerId = Guid.NewGuid();
        var currentUserId = Guid.NewGuid();
        var approval = CreateApproval(managerId: actualManagerId);
        _approvalRepositoryMock.Setup(r => r.UpdateStatusAsync(approvalId, ApprovalStatusEnum.Approved, It.IsAny<CancellationToken>()))
            .ReturnsAsync(approval);
        _approvalRepositoryMock.Setup(r => r.GetByIdAsync(approvalId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(approval);

        // Act
        var result = await _approvalService.UpdateStatus(approvalId, ApprovalStatusEnum.Approved, currentUserId);
        LogAct(result);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Be("Unauthorized");
        _approvalRepositoryMock.Verify(
            repository => repository.SaveChangesAsync(It.IsAny<CancellationToken>()),
            Times.Never
        );
        _approvalRepositoryMock.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task UpdateStatus_ShouldReturnFailure_WhenSaveChangesFails()
    {
        // Arrange
        LogArrange("Scenario", nameof(UpdateStatus_ShouldReturnFailure_WhenSaveChangesFails));

        var approvalId = Guid.NewGuid();
        var managerId = Guid.NewGuid();
        var approval = CreateApproval(managerId: managerId);
        _approvalRepositoryMock.Setup(r => r.UpdateStatusAsync(approvalId, ApprovalStatusEnum.Approved, It.IsAny<CancellationToken>()))
            .ReturnsAsync(approval);
        _approvalRepositoryMock.Setup(r => r.GetByIdAsync(approvalId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(approval);
        _approvalRepositoryMock.Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        // Act
        var result = await _approvalService.UpdateStatus(approvalId, ApprovalStatusEnum.Approved, managerId);
        LogAct(result);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Be("Failed saving Data to DB");
    }

    [Fact]
    public async Task UpdateStatus_ShouldReturnSuccessWithMappedDto_WhenAllStepsSucceed()
    {
        // Arrange
        LogArrange("Scenario", nameof(UpdateStatus_ShouldReturnSuccessWithMappedDto_WhenAllStepsSucceed));

        var approvalId = Guid.NewGuid();
        var managerId = Guid.NewGuid();
        var approval = CreateApproval(managerId: managerId);
        _approvalRepositoryMock.Setup(r => r.UpdateStatusAsync(approvalId, ApprovalStatusEnum.Approved, It.IsAny<CancellationToken>()))
            .ReturnsAsync(approval);
        _approvalRepositoryMock.Setup(r => r.GetByIdAsync(approvalId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(approval);
        _approvalRepositoryMock.Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        // Act
        var result = await _approvalService.UpdateStatus(approvalId, ApprovalStatusEnum.Approved, managerId);
        LogAct(result);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value!.Id.Should().Be(approval.Id);
    }

    #endregion
    #region AddAsync
    [Fact]
    public async Task AddAsync_ShouldReturnFailure_WhenSaveChangesFails()
    {
        // Arrange
        var dto = new ApprovalAddDTO
        {
            Type = ApprovalTypeEnum.Holiday
        };
        _approvalRepositoryMock.Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        LogArrange(nameof(dto), dto);

        // Act
        var result = await _approvalService.AddAsync(dto, Guid.NewGuid(), Guid.NewGuid());
        LogAct(result);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Be("Failed saving Data to DB");
        _approvalRepositoryMock.Verify(
            r => r.FilterAsync(
                It.IsAny<System.Linq.Expressions.Expression<Func<Approval, bool>>>(),
                It.IsAny<System.Linq.Expressions.Expression<Func<Approval, object?>>[]>(),
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
    public async Task AddAsync_ShouldReturnFailure_WhenFilterReturnsNoResults()
    {
        // Arrange
        var dto = new ApprovalAddDTO
        {
            Type = ApprovalTypeEnum.Holiday
        };
        _approvalRepositoryMock.Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        _approvalRepositoryMock.Setup(repository => repository.GetByIdAsync(
            It.IsAny<Guid>(),
            It.IsAny<System.Linq.Expressions.Expression<Func<Approval, object?>>[]>(),
            It.IsAny<CancellationToken>()
        ))
            .ReturnsAsync((Approval? )null);
        LogArrange(nameof(dto), dto);

        // Act
        var result = await _approvalService.AddAsync(dto, Guid.NewGuid(), Guid.NewGuid());
        LogAct(result);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Be("no changes happened to DB");
        _activitiesServiceMock.Verify(
            s => s.AddAsync(It.IsAny<ActivityAddDto>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()),
            Times.Never
        );
    }

    [Fact]
    public async Task AddAsync_ShouldReturnFailure_WhenAddingActivityFails()
    {
        // Arrange
        var dto = new ApprovalAddDTO
        {
            Type = ApprovalTypeEnum.Holiday
        };
        var currUserId = Guid.NewGuid();
        var managerId = Guid.NewGuid();
        var approvalWithIncludes = CreateApproval(managerId: managerId, userRequestingId: currUserId, type: ApprovalTypeEnum.Holiday);
        _approvalRepositoryMock.Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        _approvalRepositoryMock.Setup(repository => repository.GetByIdAsync(
            It.IsAny<Guid>(),
            It.IsAny<System.Linq.Expressions.Expression<Func<Approval, object?>>[]>(),
            It.IsAny<CancellationToken>()
        ))
            .ReturnsAsync(approvalWithIncludes);
        _activitiesServiceMock.Setup(s => s.AddAsync(It.IsAny<ActivityAddDto>(), currUserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<ActivityDto>.Failure("activity creation failed"));
        LogArrange(nameof(dto), dto);

        // Act
        var result = await _approvalService.AddAsync(dto, currUserId, managerId);
        LogAct(result);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Be("activity creation failed");
    }

    [Fact]
    public async Task AddAsync_ShouldReturnSuccessWithMappedDto_WhenAllStepsSucceed()
    {
        // Arrange
        var dto = new ApprovalAddDTO
        {
            Type = ApprovalTypeEnum.Holiday
        };
        var currUserId = Guid.NewGuid();
        var managerId = Guid.NewGuid();
        var approvalWithIncludes = CreateApproval(managerId: managerId, userRequestingId: currUserId, type: ApprovalTypeEnum.Holiday);
        _approvalRepositoryMock.Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        _approvalRepositoryMock.Setup(repository => repository.GetByIdAsync(
            It.IsAny<Guid>(),
            It.IsAny<System.Linq.Expressions.Expression<Func<Approval, object?>>[]>(),
            It.IsAny<CancellationToken>()
        ))
            .ReturnsAsync(approvalWithIncludes);
        _activitiesServiceMock.Setup(s => s.AddAsync(It.IsAny<ActivityAddDto>(), currUserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<ActivityDto>.Success(Fixture.Create<ActivityDto>()));
        LogArrange(nameof(dto), dto);

        // Act
        var result = await _approvalService.AddAsync(dto, currUserId, managerId);
        LogAct(result);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        _approvalRepositoryMock.Verify(
            r => r.Add(It.Is<Approval>(a => a.ManagerId == managerId && a.UserRequestingId == currUserId && a.Type == dto.Type && a.TaskId == dto.TaskId)),
            Times.Once
        );
    }

    #endregion
    #region RequestHoliday
    [Fact]
    public async Task RequestHoliday_ShouldReturnFailure_WhenUserHierarchyNotFound()
    {
        // Arrange
        LogArrange("Scenario", nameof(RequestHoliday_ShouldReturnFailure_WhenUserHierarchyNotFound));

        var currUserId = Guid.NewGuid();
        _hierarchyRepositoryMock.Setup(r => r.GetByUserIdAsync(currUserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((OrganizationHierarchy? )null);

        // Act
        var result = await _approvalService.RequestHoliday(currUserId);
        LogAct(result);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Be("User not found in organization hierarchy");
        _approvalRepositoryMock.Verify(r => r.Add(It.IsAny<Approval>()), Times.Never);
    }

    [Fact]
    public async Task RequestHoliday_ShouldReturnFailure_WhenUserHasNoParent()
    {
        // Arrange
        LogArrange("Scenario", nameof(RequestHoliday_ShouldReturnFailure_WhenUserHasNoParent));

        var currUserId = Guid.NewGuid();
        var userHierarchy = CreateHierarchyNode(currUserId, parent: null);
        _hierarchyRepositoryMock.Setup(r => r.GetByUserIdAsync(currUserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(userHierarchy);

        // Act
        var result = await _approvalService.RequestHoliday(currUserId);
        LogAct(result);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Be("missing parent in userHierarchy");
    }

    [Fact]
    public async Task RequestHoliday_ShouldCallAddAsyncWithParentAsManager_WhenParentExists()
    {
        // Arrange
        LogArrange("Scenario", nameof(RequestHoliday_ShouldCallAddAsyncWithParentAsManager_WhenParentExists));

        var currUserId = Guid.NewGuid();
        var managerId = Guid.NewGuid();
        var parentNode = CreateHierarchyNode(managerId);
        var userHierarchy = CreateHierarchyNode(currUserId, parent: parentNode);
        _hierarchyRepositoryMock.Setup(r => r.GetByUserIdAsync(currUserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(userHierarchy);
        _approvalRepositoryMock.Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        _approvalRepositoryMock.Setup(repository => repository.GetByIdAsync(
            It.IsAny<Guid>(),
            It.IsAny<System.Linq.Expressions.Expression<Func<Approval, object?>>[]>(),
            It.IsAny<CancellationToken>()
        ))
            .ReturnsAsync(CreateApproval(managerId: managerId, userRequestingId: currUserId, type: ApprovalTypeEnum.Holiday));
        _activitiesServiceMock.Setup(s => s.AddAsync(It.IsAny<ActivityAddDto>(), currUserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<ActivityDto>.Success(Fixture.Create<ActivityDto>()));

        // Act
        var result = await _approvalService.RequestHoliday(currUserId);
        LogAct(result);

        // Assert
        result.IsSuccess.Should().BeTrue();
        _approvalRepositoryMock.Verify(
            r => r.Add(It.Is<Approval>(a => a.ManagerId == managerId && a.UserRequestingId == currUserId && a.Type == ApprovalTypeEnum.Holiday)),
            Times.Once
        );
    }
    #endregion
}
