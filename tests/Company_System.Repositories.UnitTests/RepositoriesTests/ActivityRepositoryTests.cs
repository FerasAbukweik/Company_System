using AutoFixture;
using FluentAssertions;
using HR_System.Core.Domain.Entities;
using HR_System.Core.DTO.LazyLoading;
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

    #region Add

    [Fact]
    public async Task Add_ValidActivity_ShouldPersistAfterSave()
    {
        // Arrange
        var activity = CreateActivity();
        _output.WriteLine($"Adding Activity:\n{activity.ToString()}");

        // Act
        _activityRepository.Add(activity);
        await _activityRepository.SaveChangesAsync();

        // Assert
        var actual = await _activityRepository.LazyGetAllSortedAsync(new LazyDTO { Taken = 0, SectionSize = 10 });
        _output.WriteLine($"Actual Count: {actual.Count}");
        _output.WriteLine($"Actual      : {actual.FirstOrDefault()?.ToString()}");

        actual.Should().ContainSingle(a => a.Id == activity.Id);
    }

    [Fact]
    public async Task Add_WithoutSaving_ShouldNotPersist()
    {
        // Arrange
        var activity = CreateActivity();
        _output.WriteLine($"Adding Activity without saving:\n{activity.ToString()}");

        // Act — skip SaveChangesAsync intentionally
        _activityRepository.Add(activity);

        // Assert
        var actual = await _activityRepository.LazyGetAllSortedAsync(new LazyDTO { Taken = 0, SectionSize = 10 });
        _output.WriteLine($"Actual Count: {actual.Count}");

        actual.Should().BeEmpty();
    }

    [Fact]
    public async Task Add_MultipleActivities_ShouldPersistAll()
    {
        // Arrange
        var activities = CreateMany(3);
        _output.WriteLine($"Expected Count: {activities.Count}");
        activities.ForEach(a => _output.WriteLine($"  {a.ToString()}"));

        // Act
        foreach (var activity in activities)
            _activityRepository.Add(activity);
        await _activityRepository.SaveChangesAsync();

        // Assert
        var actual = await _activityRepository.LazyGetAllSortedAsync(new LazyDTO { Taken = 0, SectionSize = 10 });
        _output.WriteLine($"Actual Count: {actual.Count}");

        actual.Should().HaveCount(activities.Count);
    }

    #endregion

    #region LazyGetAllSortedAsync

    [Fact]
    public async Task LazyGetAllSortedAsync_ShouldReturnSortedByCreatedAtDescending()
    {
        // Arrange
        var oldest = CreateActivity(createdAt: DateTime.UtcNow.AddDays(-2));
        var middle = CreateActivity(createdAt: DateTime.UtcNow.AddDays(-1));
        var newest = CreateActivity(createdAt: DateTime.UtcNow);
        await SeedAsync(oldest, middle, newest);

        _output.WriteLine($"Expected Order:");
        _output.WriteLine($"  1: {newest.ToString()}");
        _output.WriteLine($"  2: {middle.ToString()}");
        _output.WriteLine($"  3: {oldest.ToString()}");

        // Act
        var actual = await _activityRepository.LazyGetAllSortedAsync(new LazyDTO { Taken = 0, SectionSize = 10 });

        _output.WriteLine($"Actual Order:");
        actual.ToList().ForEach(a => _output.WriteLine($"  {a.ToString()}"));

        // Assert
        actual.Should().HaveCount(3);
        actual[0].Id.Should().Be(newest.Id);
        actual[1].Id.Should().Be(middle.Id);
        actual[2].Id.Should().Be(oldest.Id);
    }

    [Fact]
    public async Task LazyGetAllSortedAsync_ShouldSkipCorrectly()
    {
        // Arrange
        var activities = CreateMany(5, spreadDates: true);
        await SeedAsync([.. activities]);

        var lazyData = new LazyDTO { Taken = 2, SectionSize = 10 };
        _output.WriteLine($"Total Seeded : {activities.Count}");
        _output.WriteLine($"Taken (skip) : {lazyData.Taken}");
        _output.WriteLine($"Expected Count: {activities.Count - lazyData.Taken}");

        // Act
        var actual = await _activityRepository.LazyGetAllSortedAsync(lazyData);
        _output.WriteLine($"Actual Count: {actual.Count}");

        // Assert
        actual.Should().HaveCount(activities.Count - lazyData.Taken);
    }

    [Fact]
    public async Task LazyGetAllSortedAsync_ShouldTakeCorrectly()
    {
        // Arrange
        var activities = CreateMany(10, spreadDates: true);
        await SeedAsync([.. activities]);

        var lazyData = new LazyDTO { Taken = 0, SectionSize = 4 };
        _output.WriteLine($"Total Seeded  : {activities.Count}");
        _output.WriteLine($"SectionSize   : {lazyData.SectionSize}");
        _output.WriteLine($"Expected Count: {lazyData.SectionSize}");

        // Act
        var actual = await _activityRepository.LazyGetAllSortedAsync(lazyData);
        _output.WriteLine($"Actual Count: {actual.Count}");

        // Assert
        actual.Should().HaveCount(lazyData.SectionSize);
    }

    [Fact]
    public async Task LazyGetAllSortedAsync_SkipAndTake_ShouldReturnCorrectPage()
    {
        // Arrange
        var activities = CreateMany(10, spreadDates: true);
        await SeedAsync([.. activities]);

        var lazyData = new LazyDTO { Taken = 3, SectionSize = 4 };
        _output.WriteLine($"Total Seeded  : {activities.Count}");
        _output.WriteLine($"Taken (skip)  : {lazyData.Taken}");
        _output.WriteLine($"SectionSize   : {lazyData.SectionSize}");
        _output.WriteLine($"Expected Count: {lazyData.SectionSize}");

        // Act
        var actual = await _activityRepository.LazyGetAllSortedAsync(lazyData);
        _output.WriteLine($"Actual Count: {actual.Count}");
        actual.ToList().ForEach(a => _output.WriteLine($"  {a.ToString()}"));

        // Assert
        actual.Should().HaveCount(lazyData.SectionSize);
    }

    [Fact]
    public async Task LazyGetAllSortedAsync_EmptyDatabase_ShouldReturnEmpty()
    {
        // Arrange
        var lazyData = new LazyDTO { Taken = 0, SectionSize = 10 };
        _output.WriteLine("No activities in database");

        // Act
        var actual = await _activityRepository.LazyGetAllSortedAsync(lazyData);
        _output.WriteLine($"Actual Count: {actual.Count}");

        // Assert
        actual.Should().NotBeNull();
        actual.Should().BeEmpty();
    }

    [Fact]
    public async Task LazyGetAllSortedAsync_SkipMoreThanExists_ShouldReturnEmpty()
    {
        // Arrange
        var activities = CreateMany(3);
        await SeedAsync([.. activities]);

        var lazyData = new LazyDTO { Taken = 10, SectionSize = 5 };
        _output.WriteLine($"Total Seeded  : {activities.Count}");
        _output.WriteLine($"Taken (skip)  : {lazyData.Taken}");
        _output.WriteLine("Expected Count: 0");

        // Act
        var actual = await _activityRepository.LazyGetAllSortedAsync(lazyData);
        _output.WriteLine($"Actual Count: {actual.Count}");

        // Assert
        actual.Should().BeEmpty();
    }

    [Fact]
    public async Task LazyGetAllSortedAsync_ShouldReturnReadOnlyList()
    {
        // Arrange
        var activity = CreateActivity();
        await SeedAsync(activity);

        // Act
        var actual = await _activityRepository.LazyGetAllSortedAsync(new LazyDTO { Taken = 0, SectionSize = 10 });
        _output.WriteLine($"Actual Type: {actual.GetType().Name}");

        // Assert
        actual.Should().BeAssignableTo<IReadOnlyList<Activity>>();
    }

    #endregion

    #region SaveChangesAsync

    [Fact]
    public async Task SaveChangesAsync_WithPendingChanges_ShouldReturnTrue()
    {
        // Arrange
        var activity = CreateActivity();
        _activityRepository.Add(activity);
        _output.WriteLine($"Added Activity:\n{activity.ToString()}");

        // Act
        var actual = await _activityRepository.SaveChangesAsync();
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
        var actual = await _activityRepository.SaveChangesAsync();
        _output.WriteLine($"Expected: false | Actual: {actual}");

        // Assert
        actual.Should().BeFalse();
    }

    #endregion

    #region Helpers

    private Activity CreateActivity(DateTime? createdAt = null) =>
        _fixture.Build<Activity>()
            .With(a => a.CreatedAt, createdAt ?? DateTime.UtcNow)
            .Without(a => a.TaskId)
            .Without(a => a.Task)
            .Without(a => a.ApprovalId)
            .Without(a => a.Approval)
            .Create();

    private List<Activity> CreateMany(int count, bool spreadDates = false) =>
        Enumerable.Range(0, count)
            .Select(i => CreateActivity(
                createdAt: spreadDates ? DateTime.UtcNow.AddMinutes(-i) : null))
            .ToList();

    private async Task SeedAsync(params Activity[] activities)
    {
        await _dbContext.Activities.AddRangeAsync(activities);
        await _dbContext.SaveChangesAsync();
    }

    public void Dispose() => _dbContext.Dispose();

    #endregion
}