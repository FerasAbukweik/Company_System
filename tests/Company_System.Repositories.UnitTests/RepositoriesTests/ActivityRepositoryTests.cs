using AutoFixture;
using FluentAssertions;
using HR_System.Core.Abstraction.RepositoryContracts;
using HR_System.Core.Domain.Entities;
using HR_System.Infrastructure;
using HR_System.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using Xunit.Abstractions;

namespace TestProject1.RepositoriesTests;

public class ActivityRepositoryTests : RepositoryTestBase, IDisposable
{
    private readonly IActivityRepository _activityRepository;
    private readonly ApplicationDbContext _dbContext;

    public ActivityRepositoryTests(ITestOutputHelper output) : base(output)
    {
        var dbOptions = new DbContextOptionsBuilder<ApplicationDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options;
        _dbContext = new ApplicationDbContext(dbOptions);
        _activityRepository = new ActivityRepository(_dbContext);
    }

    private Activity CreateActivity()
    {
        return Fixture.Build<Activity>()
            .Without(a => a.TriggeredBy) // avoid pulling in ApplicationUser's graph
        .Create();
    }

    #region Add
    [Fact]
    public void Add_ShouldTrackEntityAsAdded()
    {
        // Arrange
        LogArrange("Scenario", nameof(Add_ShouldTrackEntityAsAdded));

        var activity = CreateActivity();

        // Act
        _activityRepository.Add(activity);
        LogAct("Operation completed");

        // Assert
        _dbContext.Entry(activity).State.Should().Be(EntityState.Added);
        _dbContext.Activities.Local.Should().Contain(activity);
    }

    [Fact]
    public void Add_ShouldNotPersistToDatabase_BeforeSaveChangesIsCalled()
    {
        // Arrange
        LogArrange("Scenario", nameof(Add_ShouldNotPersistToDatabase_BeforeSaveChangesIsCalled));

        var activity = CreateActivity();

        // Act
        _activityRepository.Add(activity);
        LogAct("Operation completed");

        // Assert — nothing hits the store until SaveChanges runs

        // Assert
        _dbContext.Activities.AsNoTracking().Any(a => a.Id == activity.Id).Should().BeFalse();
    }

    #endregion
    #region SaveChangesAsync
    [Fact]
    public async Task SaveChangesAsync_ShouldReturnTrue_WhenThereArePendingChanges()
    {
        // Arrange
        LogArrange("Scenario", nameof(SaveChangesAsync_ShouldReturnTrue_WhenThereArePendingChanges));

        // Arrange — seed directly through the context, not via Add(),
        // so this test doesn't depend on Add() behaving correctly.
        var activity = CreateActivity();
        _dbContext.Activities.Add(activity);

        // Act
        var result = await _activityRepository.SaveChangesAsync();
        LogAct(result);

        // Assert
        result.Should().BeTrue();
        (await _dbContext.Activities.CountAsync()).Should().Be(1);
    }

    [Fact]
    public async Task SaveChangesAsync_ShouldReturnFalse_WhenThereAreNoPendingChanges()
    {
        // Arrange
        LogArrange("Scenario", nameof(SaveChangesAsync_ShouldReturnFalse_WhenThereAreNoPendingChanges));

        // Act
        var result = await _activityRepository.SaveChangesAsync();
        LogAct(result);

        // Assert
        result.Should().BeFalse();
    }

    #endregion
    public void Dispose() => _dbContext.Dispose();
}
