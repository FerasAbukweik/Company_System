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