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

public class ApplicationUsersRepositoryTests : IDisposable
{
    private readonly IApplicationUsersRepository _usersRepository;
    private readonly ApplicationDbContext _dbContext;
    private readonly ITestOutputHelper _output;
    private readonly IFixture _fixture;

    public ApplicationUsersRepositoryTests(ITestOutputHelper output)
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
        _usersRepository = new ApplicationUsersesRepository(_dbContext);
    }

    private ApplicationUser CreateUser(string? userName = null, PositionsEnum? position = null)
    {
        return _fixture.Build<ApplicationUser>()
            .With(u => u.UserName, userName ?? $"user_{Guid.NewGuid():N}")
            .With(u => u.Position, position ?? PositionsEnum.Employee) // adjust to a real enum member if this name differs
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

    #region FilterAsync

    [Fact]
    public async Task FilterAsync_ShouldReturnOnlyUsersMatchingPredicate()
    {
        // Arrange
        var matching = CreateUser("matching_user");
        var nonMatching = CreateUser("other_user");

        _dbContext.Users.AddRange(matching, nonMatching);
        await _dbContext.SaveChangesAsync();

        // Act
        var result = await _usersRepository.FilterAsync(u => u.UserName == "matching_user");

        // Assert
        result.Should().ContainSingle(u => u.Id == matching.Id);
        result.Should().NotContain(u => u.Id == nonMatching.Id);
    }

    [Fact]
    public async Task FilterAsync_ShouldReturnEmptyList_WhenNoUsersMatchPredicate()
    {
        // Arrange
        _dbContext.Users.Add(CreateUser());
        await _dbContext.SaveChangesAsync();

        // Act
        var result = await _usersRepository.FilterAsync(u => u.UserName == "nonexistent_user");

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task FilterAsync_ShouldReturnUntrackedEntities()
    {
        // Arrange
        var user = CreateUser();
        _dbContext.Users.Add(user);
        await _dbContext.SaveChangesAsync();
        _dbContext.ChangeTracker.Clear();

        // Act
        var result = await _usersRepository.FilterAsync(u => u.Id == user.Id);

        // Assert
        var fetched = result.Single();
        _dbContext.Entry(fetched).State.Should().Be(EntityState.Detached);
    }

    [Fact]
    public async Task FilterAsync_ShouldLoadIncludedNavigationProperty_WhenIncludesProvided()
    {
        // Arrange
        var user = CreateUser();
        _dbContext.Users.Add(user);
        await _dbContext.SaveChangesAsync();

        var activity = _fixture.Build<Activity>()
            .With(a => a.TriggeredById, user.Id)
            .Without(a => a.TriggeredBy)
            .Create();
        _dbContext.Activities.Add(activity);
        await _dbContext.SaveChangesAsync();
        _dbContext.ChangeTracker.Clear();

        // Act
        var result = await _usersRepository.FilterAsync(
            checks: u => u.Id == user.Id,
            includes: [u => u.Activities]);

        // Assert
        var fetched = result.Single();
        fetched.Activities.Should().ContainSingle(a => a.Id == activity.Id);
    }

    [Fact]
    public async Task FilterAsync_ShouldNotLoadNavigationProperty_WhenIncludesNotProvided()
    {
        // Arrange
        var user = CreateUser();
        _dbContext.Users.Add(user);
        await _dbContext.SaveChangesAsync();

        var activity = _fixture.Build<Activity>()
            .With(a => a.TriggeredById, user.Id)
            .Without(a => a.TriggeredBy)
            .Create();
        _dbContext.Activities.Add(activity);
        await _dbContext.SaveChangesAsync();
        _dbContext.ChangeTracker.Clear();

        // Act
        var result = await _usersRepository.FilterAsync(u => u.Id == user.Id);

        // Assert
        var fetched = result.Single();
        fetched.Activities.Should().BeEmpty();
    }

    #endregion

    #region SaveChangesAsync

    [Fact]
    public async Task SaveChangesAsync_ShouldReturnTrue_WhenThereArePendingChanges()
    {
        // Arrange
        _dbContext.Users.Add(CreateUser());

        // Act
        var result = await _usersRepository.SaveChangesAsync();

        // Assert
        result.Should().BeTrue();
        (await _dbContext.Users.CountAsync()).Should().Be(1);
    }

    [Fact]
    public async Task SaveChangesAsync_ShouldReturnFalse_WhenThereAreNoPendingChanges()
    {
        // Act
        var result = await _usersRepository.SaveChangesAsync();

        // Assert
        result.Should().BeFalse();
    }

    #endregion

    public void Dispose() => _dbContext.Dispose();
}