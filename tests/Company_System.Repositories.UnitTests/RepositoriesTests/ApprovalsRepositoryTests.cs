using System.Linq.Expressions;
using AutoFixture;
using FluentAssertions;
using HR_System.Core.Domain.Entities;
using HR_System.Core.DTO.LazyLoading;
using HR_System.Core.Enums;
using HR_System.Core.Interfaces.RepositoryContracts;
using HR_System.Infrastructure;
using HR_System.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using Xunit.Abstractions;

namespace TestProject1.RepositoriesTests;

public class ApprovalRepositoryTests : IDisposable
{
    private readonly IApprovalRepository _approvalRepository;
    private readonly ApplicationDbContext _dbContext;
    private readonly ITestOutputHelper _output;
    private readonly IFixture _fixture;

    public ApprovalRepositoryTests(ITestOutputHelper output)
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
        _approvalRepository = new ApprovalRepository(_dbContext);
    }

    private Approval CreateApproval(
        ApprovalStatusEnum? status = null,
        Guid? taskId = null,
        DateTime? createdOn = null)
    {
        return _fixture.Build<Approval>()
            .With(a => a.Status, status ?? ApprovalStatusEnum.Pending)
            .With(a => a.TaskId, taskId)
            .With(a => a.CreatedOn, createdOn ?? DateTime.UtcNow)
            .Without(a => a.Task)
            .Without(a => a.UserRequesting)
            .Without(a => a.Manager)
            .Create();
    }

    private AppTask CreateTask(Guid? id = null)
    {
        var task = _fixture.Build<AppTask>()
            .Without(t => t.User)
            .Without(t => t.Manager)
            .Without(t => t.Approvals)
            .Create();

        if (id.HasValue)
            task.Id = id.Value;

        return task;
    }

    #region UpdateStatus

    [Fact]
    public async Task UpdateStatus_ShouldUpdateStatus_WhenApprovalExists()
    {
        // Arrange
        var approval = CreateApproval(status: ApprovalStatusEnum.Pending);
        _dbContext.Approvals.Add(approval);
        await _dbContext.SaveChangesAsync();

        // Act
        var result = await _approvalRepository.UpdateStatus(approval.Id, ApprovalStatusEnum.Approved);

        // Assert
        result.Should().NotBeNull();
        result!.Status.Should().Be(ApprovalStatusEnum.Approved);
    }

    [Fact]
    public async Task UpdateStatus_ShouldPersistUpdatedStatus_AfterSaveChanges()
    {
        // Arrange
        var approval = CreateApproval(status: ApprovalStatusEnum.Pending);
        _dbContext.Approvals.Add(approval);
        await _dbContext.SaveChangesAsync();

        // Act
        await _approvalRepository.UpdateStatus(approval.Id, ApprovalStatusEnum.Rejected);
        await _dbContext.SaveChangesAsync();
        _dbContext.ChangeTracker.Clear();

        // Assert
        var fromDb = await _dbContext.Approvals.SingleAsync(a => a.Id == approval.Id);
        fromDb.Status.Should().Be(ApprovalStatusEnum.Rejected);
    }

    [Fact]
    public async Task UpdateStatus_ShouldReturnNull_WhenApprovalDoesNotExist()
    {
        // Act
        var result = await _approvalRepository.UpdateStatus(Guid.NewGuid(), ApprovalStatusEnum.Approved);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task UpdateStatus_ShouldLoadRelatedTask()
    {
        // Arrange
        var task = CreateTask();
        _dbContext.Tasks.Add(task);

        var approval = CreateApproval(taskId: task.Id);
        _dbContext.Approvals.Add(approval);
        await _dbContext.SaveChangesAsync();
        _dbContext.ChangeTracker.Clear();

        // Act
        var result = await _approvalRepository.UpdateStatus(approval.Id, ApprovalStatusEnum.Approved);

        // Assert
        result.Should().NotBeNull();
        result!.Task.Should().NotBeNull();
        result.Task!.Id.Should().Be(task.Id);
    }

    #endregion

    #region Add

    [Fact]
    public void Add_ShouldTrackEntityAsAdded()
    {
        // Arrange
        var approval = CreateApproval();

        // Act
        _approvalRepository.Add(approval);

        // Assert
        _dbContext.Entry(approval).State.Should().Be(EntityState.Added);
        _dbContext.Approvals.Local.Should().Contain(approval);
    }

    [Fact]
    public void Add_ShouldNotPersistToDatabase_BeforeSaveChangesIsCalled()
    {
        // Arrange
        var approval = CreateApproval();

        // Act
        _approvalRepository.Add(approval);

        // Assert
        _dbContext.Approvals.AsNoTracking().Any(a => a.Id == approval.Id).Should().BeFalse();
    }

    #endregion

    #region FilterAsync

    [Fact]
    public async Task FilterAsync_ShouldReturnOnlyApprovalsMatchingFilter()
    {
        // Arrange
        var matching = CreateApproval(status: ApprovalStatusEnum.Approved);
        var nonMatching = CreateApproval(status: ApprovalStatusEnum.Pending);

        _dbContext.Approvals.AddRange(matching, nonMatching);
        await _dbContext.SaveChangesAsync();

        // Act
        var result = await _approvalRepository.FilterAsync(a => a.Status == ApprovalStatusEnum.Approved);

        // Assert
        result.Should().ContainSingle(a => a.Id == matching.Id);
        result.Should().NotContain(a => a.Id == nonMatching.Id);
    }

    [Fact]
    public async Task FilterAsync_ShouldReturnEmptyList_WhenNoApprovalsMatchFilter()
    {
        // Arrange
        _dbContext.Approvals.Add(CreateApproval(status: ApprovalStatusEnum.Pending));
        await _dbContext.SaveChangesAsync();

        // Act
        var result = await _approvalRepository.FilterAsync(a => a.Status == ApprovalStatusEnum.Rejected);

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task FilterAsync_ShouldReturnUntrackedEntities()
    {
        // Arrange
        var approval = CreateApproval();
        _dbContext.Approvals.Add(approval);
        await _dbContext.SaveChangesAsync();
        _dbContext.ChangeTracker.Clear();

        // Act
        var result = await _approvalRepository.FilterAsync(a => a.Id == approval.Id);

        // Assert
        var fetched = result.Single();
        _dbContext.Entry(fetched).State.Should().Be(EntityState.Detached);
    }

    [Fact]
    public async Task FilterAsync_ShouldLoadIncludedNavigationProperty_WhenIncludeProvided()
    {
        // Arrange
        var task = CreateTask();
        _dbContext.Tasks.Add(task);

        var approval = CreateApproval(taskId: task.Id);
        _dbContext.Approvals.Add(approval);
        await _dbContext.SaveChangesAsync();
        _dbContext.ChangeTracker.Clear();

        // Act
        var result = await _approvalRepository.FilterAsync(
            filter: a => a.Id == approval.Id,
            include: [a => a.Task!]);

        // Assert
        var fetched = result.Single();
        fetched.Task.Should().NotBeNull();
        fetched.Task!.Id.Should().Be(task.Id);
    }

    [Fact]
    public async Task FilterAsync_ShouldNotLoadNavigationProperty_WhenIncludeNotProvided()
    {
        // Arrange
        var task = CreateTask();
        _dbContext.Tasks.Add(task);

        var approval = CreateApproval(taskId: task.Id);
        _dbContext.Approvals.Add(approval);
        await _dbContext.SaveChangesAsync();
        _dbContext.ChangeTracker.Clear();

        // Act
        var result = await _approvalRepository.FilterAsync(a => a.Id == approval.Id);

        // Assert
        var fetched = result.Single();
        fetched.Task.Should().BeNull();
    }

    #endregion

    #region LazyGetApprovals

    [Fact]
    public async Task LazyGetApprovals_ShouldReturnOnlyApprovalsMatchingFilter()
    {
        // Arrange
        var matching = CreateApproval(status: ApprovalStatusEnum.Approved);
        var nonMatching = CreateApproval(status: ApprovalStatusEnum.Pending);

        _dbContext.Approvals.AddRange(matching, nonMatching);
        await _dbContext.SaveChangesAsync();

        var lazyData = new LazyDTO { Taken = 0, SectionSize = 10 };

        // Act
        var result = await _approvalRepository.LazyGetApprovals(
            lazyData, a => a.Status == ApprovalStatusEnum.Approved);

        // Assert
        result.Should().ContainSingle(a => a.Id == matching.Id);
        result.Should().NotContain(a => a.Id == nonMatching.Id);
    }

    [Fact]
    public async Task LazyGetApprovals_ShouldReturnApprovalsOrderedByCreatedOnDescending()
    {
        // Arrange
        var oldest = CreateApproval(createdOn: DateTime.UtcNow.AddDays(-2));
        var middle = CreateApproval(createdOn: DateTime.UtcNow.AddDays(-1));
        var newest = CreateApproval(createdOn: DateTime.UtcNow);

        _dbContext.Approvals.AddRange(oldest, middle, newest);
        await _dbContext.SaveChangesAsync();

        var lazyData = new LazyDTO { Taken = 0, SectionSize = 10 };

        // Act
        var result = await _approvalRepository.LazyGetApprovals(lazyData, _ => true);

        // Assert
        result.Select(a => a.Id).Should().ContainInOrder(newest.Id, middle.Id, oldest.Id);
    }

    [Fact]
    public async Task LazyGetApprovals_ShouldRespectSkipAndTake()
    {
        // Arrange
        var approvals = Enumerable.Range(0, 5)
            .Select(i => CreateApproval(createdOn: DateTime.UtcNow.AddMinutes(-i)))
            .ToList();

        _dbContext.Approvals.AddRange(approvals);
        await _dbContext.SaveChangesAsync();

        var lazyData = new LazyDTO { Taken = 1, SectionSize = 2 };

        // Act
        var result = await _approvalRepository.LazyGetApprovals(lazyData, _ => true);

        // Assert — sorted desc by CreatedOn: [0,1,2,3,4] -> skip 1, take 2 -> [1,2]
        result.Should().HaveCount(2);
        result.Select(a => a.Id).Should().ContainInOrder(approvals[1].Id, approvals[2].Id);
    }

    [Fact]
    public async Task LazyGetApprovals_ShouldLoadIncludedNavigationProperty_WhenIncludeProvided()
    {
        // Arrange
        var task = CreateTask();
        _dbContext.Tasks.Add(task);

        var approval = CreateApproval(taskId: task.Id);
        _dbContext.Approvals.Add(approval);
        await _dbContext.SaveChangesAsync();
        _dbContext.ChangeTracker.Clear();

        var lazyData = new LazyDTO { Taken = 0, SectionSize = 10 };

        // Act
        var result = await _approvalRepository.LazyGetApprovals(
            lazyData, a => a.Id == approval.Id, include: [a => a.Task!]);

        // Assert
        var fetched = result.Single();
        fetched.Task.Should().NotBeNull();
        fetched.Task!.Id.Should().Be(task.Id);
    }

    [Fact]
    public async Task LazyGetApprovals_ShouldReturnEmptyList_WhenNoApprovalsMatchFilter()
    {
        // Arrange
        _dbContext.Approvals.Add(CreateApproval(status: ApprovalStatusEnum.Pending));
        await _dbContext.SaveChangesAsync();

        var lazyData = new LazyDTO { Taken = 0, SectionSize = 10 };

        // Act
        var result = await _approvalRepository.LazyGetApprovals(
            lazyData, a => a.Status == ApprovalStatusEnum.Rejected);

        // Assert
        result.Should().BeEmpty();
    }

    #endregion

    #region SaveChangesAsync

    [Fact]
    public async Task SaveChangesAsync_ShouldReturnTrue_WhenThereArePendingChanges()
    {
        // Arrange
        _dbContext.Approvals.Add(CreateApproval());

        // Act
        var result = await _approvalRepository.SaveChangesAsync();

        // Assert
        result.Should().BeTrue();
        (await _dbContext.Approvals.CountAsync()).Should().Be(1);
    }

    [Fact]
    public async Task SaveChangesAsync_ShouldReturnFalse_WhenThereAreNoPendingChanges()
    {
        // Act
        var result = await _approvalRepository.SaveChangesAsync();

        // Assert
        result.Should().BeFalse();
    }

    #endregion

    public void Dispose() => _dbContext.Dispose();
}