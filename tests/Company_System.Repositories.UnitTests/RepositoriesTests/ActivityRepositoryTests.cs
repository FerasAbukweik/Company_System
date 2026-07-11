using AutoFixture;
using FluentAssertions;
using HR_System.Core.Domain.Entities;
using HR_System.Core.Interfaces.RepositoryContracts;
using HR_System.Infrastructure;
using HR_System.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using Xunit.Abstractions;

namespace TestProject1.RepositoriesTests;

public class ActivityRepositoryTests : IDisposable
{
    private readonly IActivityRepository _activityRepository;
    private readonly ApplicationDbContext _dbContext;
    private readonly ITestOutputHelper _output;
    private readonly IFixture _fixture;

    public ActivityRepositoryTests(ITestOutputHelper output)
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
        _activityRepository = new ActivityRepository(_dbContext);
    }

    private Activity CreateActivity()
    {
        return _fixture.Build<Activity>()
            .Without(a => a.TriggeredBy) // avoid pulling in ApplicationUser's graph
            .Create();
    }

    #region Add

    [Fact]
    public void Add_ShouldTrackEntityAsAdded()
    {
        // Arrange
        var activity = CreateActivity();

        // Act
        _activityRepository.Add(activity);

        // Assert
        _dbContext.Entry(activity).State.Should().Be(EntityState.Added);
        _dbContext.Activities.Local.Should().Contain(activity);
    }

    [Fact]
    public void Add_ShouldNotPersistToDatabase_BeforeSaveChangesIsCalled()
    {
        // Arrange
        var activity = CreateActivity();

        // Act
        _activityRepository.Add(activity);

        // Assert — nothing hits the store until SaveChanges runs
        _dbContext.Activities.AsNoTracking().Any(a => a.Id == activity.Id).Should().BeFalse();
    }

    #endregion

    #region SaveChangesAsync

    [Fact]
    public async Task SaveChangesAsync_ShouldReturnTrue_WhenThereArePendingChanges()
    {
        // Arrange — seed directly through the context, not via Add(),
        // so this test doesn't depend on Add() behaving correctly.
        var activity = CreateActivity();
        _dbContext.Activities.Add(activity);

        // Act
        var result = await _activityRepository.SaveChangesAsync();

        // Assert
        result.Should().BeTrue();
        (await _dbContext.Activities.CountAsync()).Should().Be(1);
    }

    [Fact]
    public async Task SaveChangesAsync_ShouldReturnFalse_WhenThereAreNoPendingChanges()
    {
        // Act
        var result = await _activityRepository.SaveChangesAsync();

        // Assert
        result.Should().BeFalse();
    }

    #endregion
    

    public void Dispose() => _dbContext.Dispose();
}