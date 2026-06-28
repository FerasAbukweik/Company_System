using AutoFixture;
using EntityFrameworkCore.Testing.Moq;
using FluentAssertions;
using Company_System.Infrastructure;
using HR_System.Core.Domain.Identity;
using HR_System.Core.Interfaces.RepositoryContracts;
using HR_System.Infrastructure;
using HR_System.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using Xunit.Abstractions;

namespace TestProject1.RepositoriesTests;

public class ApplicationUsersesRepositoryTests : IDisposable
{
    private readonly IApplicationUsersRepository _applicationUsersRepository;
    private readonly ApplicationDbContext _dbContext;
    private readonly ITestOutputHelper _output;
    private readonly IFixture _fixture;

    public ApplicationUsersesRepositoryTests(ITestOutputHelper output)
    {
        _output = output;

        _fixture = new Fixture();
        _fixture.Behaviors.OfType<ThrowingRecursionBehavior>().ToList()
            .ForEach(e => _fixture.Behaviors.Remove(e));
        _fixture.Behaviors.Add(new OmitOnRecursionBehavior());

        var dbContextOptions = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        // ✅ Keep MockedDbContext — needed for BeginTransactionAsync
        _dbContext = Create.MockedDbContextFor<ApplicationDbContext>(dbContextOptions);
        _applicationUsersRepository = new ApplicationUsersesRepository(_dbContext);
    }

    #region FilterAsync

    [Fact]
    public async Task FilterAsync_MatchingSingleUser_ShouldReturnOnlyThatUser()
    {
        // Arrange
        var targetUser = CreateUser(fullName: "Target User");
        var otherUsers = CreateMany(5);
        await SeedAsync([targetUser, .. otherUsers]);

        _output.WriteLine($"Expected Id  : {targetUser.Id}");
        _output.WriteLine($"Expected Name: {targetUser.FullName}");

        // Act
        var actual = await _applicationUsersRepository.FilterAsync(u => u.FullName == "Target User");
        _output.WriteLine($"Actual Count: {actual.Count}");
        actual.ToList().ForEach(u => _output.WriteLine($"  Actual: {u.Id} | {u.FullName}"));

        // Assert
        actual.Should().NotBeNull();
        actual.Should().ContainSingle();
        actual[0].Id.Should().Be(targetUser.Id);
        actual[0].FullName.Should().Be(targetUser.FullName);
    }

    [Fact]
    public async Task FilterAsync_MatchingMultipleUsers_ShouldReturnAllMatches()
    {
        // Arrange — filter by FullName prefix since it's the only custom property
        var sharedName = "John";
        var matchingUsers = CreateMany(3, fullName: sharedName);
        var otherUsers = CreateMany(3, fullName: "Other");
        await SeedAsync([.. matchingUsers, .. otherUsers]);

        _output.WriteLine($"Expected Count: {matchingUsers.Count}");
        matchingUsers.ForEach(u => _output.WriteLine($"  Expected: {u.Id} | {u.FullName}"));

        // Act
        var actual = await _applicationUsersRepository.FilterAsync(u => u.FullName == sharedName);
        _output.WriteLine($"Actual Count: {actual.Count}");
        actual.ToList().ForEach(u => _output.WriteLine($"  Actual: {u.Id} | {u.FullName}"));

        // Assert
        actual.Should().NotBeNull();
        actual.Should().HaveCount(matchingUsers.Count);
        actual.Should().OnlyContain(u => u.FullName == sharedName);
    }

    [Fact]
    public async Task FilterAsync_NoMatchingUsers_ShouldReturnEmpty()
    {
        // Arrange
        var users = CreateMany(5);
        await SeedAsync([.. users]);

        _output.WriteLine("Filtering for non-existent name");
        _output.WriteLine("Expected Count: 0");

        // Act
        var actual = await _applicationUsersRepository.FilterAsync(u => u.FullName == "Does Not Exist");
        _output.WriteLine($"Actual Count: {actual.Count}");

        // Assert
        actual.Should().NotBeNull();
        actual.Should().BeEmpty();
    }

    [Fact]
    public async Task FilterAsync_EmptyDatabase_ShouldReturnEmpty()
    {
        // Arrange — nothing seeded
        _output.WriteLine("No users in database");
        _output.WriteLine("Expected Count: 0");

        // Act
        var actual = await _applicationUsersRepository.FilterAsync(u => true);
        _output.WriteLine($"Actual Count: {actual.Count}");

        // Assert
        actual.Should().NotBeNull();
        actual.Should().BeEmpty();
    }

    [Fact]
    public async Task FilterAsync_ShouldReturnReadOnlyList()
    {
        // Arrange
        var user = CreateUser();
        await SeedAsync(user);

        // Act
        var actual = await _applicationUsersRepository.FilterAsync(u => true);
        _output.WriteLine($"Actual Type: {actual.GetType().Name}");

        // Assert
        actual.Should().BeAssignableTo<IReadOnlyList<ApplicationUser>>();
    }

    #endregion

    #region BeginTransactionAsync

    [Fact]
    public async Task BeginTransactionAsync_ShouldReturnTransaction()
    {
        // Act
        var actual = await _applicationUsersRepository.BeginTransactionAsync();
        _output.WriteLine($"Transaction: {actual?.GetType().Name ?? "null"}");

        // Assert
        actual.Should().NotBeNull();
    }

    #endregion

    #region SaveChangesAsync

    [Fact]
    public async Task SaveChangesAsync_WithNoChanges_ShouldReturnFalse()
    {
        // Arrange
        _output.WriteLine("No changes made");

        // Act
        var actual = await _applicationUsersRepository.SaveChangesAsync();
        _output.WriteLine($"Expected: false | Actual: {actual}");

        // Assert
        actual.Should().BeFalse();
    }

    #endregion

    #region Helpers

    private ApplicationUser CreateUser(string? fullName = null) =>
        _fixture.Build<ApplicationUser>()
            .With(u => u.FullName, fullName ?? _fixture.Create<string>())
            .Without(u => u.Tasks)
            .Without(u => u.CreatedTasks)
            .Without(u => u.RefreshTokens)
            .Without(u => u.Approvals)
            .Without(u => u.ToApprove)
            .Create();

    private List<ApplicationUser> CreateMany(int count, string? fullName = null) =>
        Enumerable.Range(0, count)
            .Select(_ => CreateUser(fullName))
            .ToList();

    private async Task SeedAsync(params ApplicationUser[] users)
    {
        await _dbContext.Users.AddRangeAsync(users);
        await _dbContext.SaveChangesAsync();
    }

    public void Dispose() => _dbContext.Dispose();

    #endregion
}