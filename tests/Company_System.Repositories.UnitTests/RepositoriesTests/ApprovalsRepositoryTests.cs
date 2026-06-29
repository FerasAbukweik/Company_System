using AutoFixture;
using FluentAssertions;
using HR_System.Core.Domain.Entities;
using HR_System.Core.Domain.Identity;
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

    #region Add

    [Fact]
    public async Task Add_ValidApproval_ShouldPersistAfterSave()
    {
        // Arrange
        var approval = CreateApproval();
        _output.WriteLine($"Adding Approval: {approval.Id} | {approval.Type}");

        // Act
        _approvalRepository.Add(approval);
        await _approvalRepository.SaveChangesAsync();

        // Assert
        var actual = await _approvalRepository.GetNeedsApprovalAsync(approval.ManagerId);
        _output.WriteLine($"Actual Count: {actual.Count}");
        _output.WriteLine($"Actual Id   : {actual.FirstOrDefault()?.Id}");

        actual.Should().ContainSingle(a => a.Id == approval.Id);
    }

    [Fact]
    public async Task Add_WithoutSaving_ShouldNotPersist()
    {
        // Arrange
        var approval = CreateApproval();
        _output.WriteLine($"Adding Approval without saving: {approval.Id}");

        // Act — skip SaveChangesAsync intentionally
        _approvalRepository.Add(approval);

        // Assert
        var actual = await _approvalRepository.GetNeedsApprovalAsync(approval.ManagerId);
        _output.WriteLine($"Actual Count: {actual.Count}");

        actual.Should().BeEmpty();
    }

    [Fact]
    public async Task Add_MultipleApprovals_ShouldPersistAll()
    {
        // Arrange
        var managerId = Guid.NewGuid();
        var approvals = CreateMany(3, managerId: managerId);
        _output.WriteLine($"Expected Count: {approvals.Count}");
        approvals.ForEach(a => _output.WriteLine($"  Approval: {a.Id} | {a.Type}"));

        // Act
        foreach (var approval in approvals)
            _approvalRepository.Add(approval);
        await _approvalRepository.SaveChangesAsync();

        // Assert
        var actual = await _approvalRepository.GetNeedsApprovalAsync(managerId);
        _output.WriteLine($"Actual Count: {actual.Count}");

        actual.Should().HaveCount(approvals.Count);
    }

    #endregion

    #region GetManagerToApprove

    [Fact]
    public async Task GetManagerToApprove_ManagerWithApprovals_ShouldReturnOnlyManagerApprovals()
    {
        // Arrange
        var managerId = Guid.NewGuid();
        var managerApprovals = CreateMany(3, managerId: managerId);
        var otherApprovals = CreateMany(3); // belong to random managerIds
        await SeedAsync([.. managerApprovals, .. otherApprovals]);

        _output.WriteLine($"ManagerId     : {managerId}");
        _output.WriteLine($"Expected Count: {managerApprovals.Count}");
        managerApprovals.ForEach(a => _output.WriteLine($"  Expected: {a.Id} | ManagerId: {a.ManagerId}"));

        // Act
        var actual = await _approvalRepository.GetNeedsApprovalAsync(managerId);
        _output.WriteLine($"Actual Count: {actual.Count}");
        actual.ToList().ForEach(a => _output.WriteLine($"  Actual: {a.Id} | ManagerId: {a.ManagerId}"));

        // Assert
        actual.Should().NotBeNull();
        actual.Should().HaveCount(managerApprovals.Count);
        actual.Should().OnlyContain(a => a.ManagerId == managerId);
    }

    [Fact]
    public async Task GetManagerToApprove_ManagerWithNoApprovals_ShouldReturnEmpty()
    {
        // Arrange
        var managerId = Guid.NewGuid();
        var otherApprovals = CreateMany(3);
        await SeedAsync([.. otherApprovals]);

        _output.WriteLine($"ManagerId     : {managerId}");
        _output.WriteLine("Expected Count: 0");

        // Act
        var actual = await _approvalRepository.GetNeedsApprovalAsync(managerId);
        _output.WriteLine($"Actual Count: {actual.Count}");

        // Assert
        actual.Should().NotBeNull();
        actual.Should().BeEmpty();
    }

    [Fact]
    public async Task GetManagerToApprove_ShouldReturnReadOnlyList()
    {
        // Arrange
        var managerId = Guid.NewGuid();
        var approval = CreateApproval(managerId: managerId);
        await SeedAsync(approval);

        // Act
        var actual = await _approvalRepository.GetNeedsApprovalAsync(managerId);
        _output.WriteLine($"Actual Type: {actual.GetType().Name}");

        // Assert
        actual.Should().BeAssignableTo<IReadOnlyList<Approval>>();
    }

    #endregion

    #region UpdateStatus

    [Fact]
    public async Task UpdateStatus_ValidApproval_ShouldReturnApprovalWithNewStatus()
    {
        // Arrange
        var approval = CreateApproval(status: ApprovalStatusEnum.Pending);
        await SeedAsync(approval);
        var newStatus = ApprovalStatusEnum.Approved;

        _output.WriteLine($"Approval Id    : {approval.Id}");
        _output.WriteLine($"Initial Status : {approval.Status}");
        _output.WriteLine($"Expected Status: {newStatus}");

        // Act
        var actual = await _approvalRepository.UpdateStatus(approval.Id, newStatus);
        _output.WriteLine($"Actual Status: {actual?.Status}");

        // Assert
        actual.Should().NotBeNull();
        actual!.Status.Should().Be(newStatus);
    }

    [Fact]
    public async Task UpdateStatus_ValidApproval_ShouldPersistAfterSave()
    {
        // Arrange
        var managerId = Guid.NewGuid();
        var approval = CreateApproval(managerId: managerId, status: ApprovalStatusEnum.Pending);
        await SeedAsync(approval);
        var newStatus = ApprovalStatusEnum.Approved;

        _output.WriteLine($"Approval Id    : {approval.Id}");
        _output.WriteLine($"Expected Status: {newStatus}");

        // Act
        await _approvalRepository.UpdateStatus(approval.Id, newStatus);
        await _approvalRepository.SaveChangesAsync();

        // Assert — re-fetch via GetManagerToApprove
        var approvals = await _approvalRepository.GetNeedsApprovalAsync(managerId);
        var actual = approvals.Single(a => a.Id == approval.Id);
        _output.WriteLine($"Actual Status (persisted): {actual.Status}");

        actual.Status.Should().Be(newStatus);
    }

    [Fact]
    public async Task UpdateStatus_NonExistentApproval_ShouldReturnNull()
    {
        // Arrange
        var nonExistentId = Guid.NewGuid();
        _output.WriteLine($"Non-existent Approval Id: {nonExistentId}");

        // Act
        var actual = await _approvalRepository.UpdateStatus(nonExistentId, ApprovalStatusEnum.Approved);
        _output.WriteLine($"Actual: {actual?.ToString() ?? "null"}");

        // Assert
        actual.Should().BeNull();
    }

    [Fact]
    public async Task UpdateStatus_ShouldNotAffectOtherApprovals()
    {
        // Arrange
        var managerId = Guid.NewGuid();
        var approval1 = CreateApproval(managerId: managerId, status: ApprovalStatusEnum.Pending);
        var approval2 = CreateApproval(managerId: managerId, status: ApprovalStatusEnum.Pending);
        await SeedAsync(approval1, approval2);

        _output.WriteLine($"Updating Approval1: {approval1.Id} → Approved");
        _output.WriteLine($"Approval2 should stay Pending: {approval2.Id}");

        // Act
        await _approvalRepository.UpdateStatus(approval1.Id, ApprovalStatusEnum.Approved);
        await _approvalRepository.SaveChangesAsync();

        // Assert
        var approvals = await _approvalRepository.GetNeedsApprovalAsync(managerId);
        var actual = approvals.Single(a => a.Id == approval2.Id);

        _output.WriteLine($"Approval2 Expected: {ApprovalStatusEnum.Pending}");
        _output.WriteLine($"Approval2 Actual  : {actual.Status}");

        actual.Status.Should().Be(ApprovalStatusEnum.Pending);
    }

    #endregion

    #region SaveChangesAsync

    [Fact]
    public async Task SaveChangesAsync_WithPendingChanges_ShouldReturnTrue()
    {
        // Arrange
        var approval = CreateApproval();
        _approvalRepository.Add(approval);
        _output.WriteLine($"Added Approval: {approval.Id}");

        // Act
        var actual = await _approvalRepository.SaveChangesAsync();
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
        var actual = await _approvalRepository.SaveChangesAsync();
        _output.WriteLine($"Expected: false | Actual: {actual}");

        // Assert
        actual.Should().BeFalse();
    }

    #endregion

    #region Helpers

    private Approval CreateApproval(Guid? managerId = null, ApprovalStatusEnum? status = null) =>
        _fixture.Build<Approval>()
            .With(a => a.ManagerId, managerId ?? Guid.NewGuid())
            .With(a => a.Manager, null as ApplicationUser)
            .With(a => a.Status, status ?? _fixture.Create<ApprovalStatusEnum>())
            .Create();

    private List<Approval> CreateMany(int count, Guid? managerId = null) =>
        Enumerable.Range(0, count)
            .Select(_ => CreateApproval(managerId))
            .ToList();

    private async Task SeedAsync(params Approval[] approvals)
    {
        await _dbContext.Approvals.AddRangeAsync(approvals);
        await _dbContext.SaveChangesAsync();
    }

    public void Dispose() => _dbContext.Dispose();

    #endregion
}