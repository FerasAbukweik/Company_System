using AutoFixture;
using EntityFrameworkCore.Testing.Moq;
using FluentAssertions;
using HR_System.Core.Domain.Entities;
using HR_System.Core.Interfaces.RepositoryContracts;
using HR_System.Infrastructure;
using HR_System.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using Xunit.Abstractions;

namespace TestProject1.RepositoriesTests;

public class TasksRepositoryTests
{
    private readonly  IAppTasksRepository _tasksRepository;
    private readonly ApplicationDbContext _dbContext;
    private readonly IFixture _fixture;
    private readonly ITestOutputHelper _output;
    

    public TasksRepositoryTests(ITestOutputHelper output)
    {
        _output = output;
        
        _fixture = new Fixture();
        _fixture.Behaviors.OfType<ThrowingRecursionBehavior>().ToList()
            .ForEach(b => _fixture.Behaviors.Remove(b));
        _fixture.Behaviors.Add(new OmitOnRecursionBehavior());
        
        var dbOptions = new DbContextOptionsBuilder()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        
        _dbContext = Create.MockedDbContextFor<ApplicationDbContext>(dbOptions);

        _tasksRepository = new AppTasksesRepository(_dbContext);
    }

    #region GetUserTasksTests

    [Fact]
    public async Task GetUserTasks_ValidData_ShouldSucceed()
    {
        // Arrange
        var userId = Guid.NewGuid();
        
        var initalData = _fixture.CreateMany<AppTask>(10).ToArray();
        for (int i = 0; i < initalData.Length / 2; i++)
        {
            initalData[i].UserId = userId;
            initalData[i].User = null;
        }
        
        _dbContext.Tasks.AddRange(initalData);
        _dbContext.SaveChanges();
        
        var expected = initalData[..(initalData.Length/2)];
        
        _output.WriteLine($"userId: {userId.ToString()}");
        foreach (var item in expected)
            _output.WriteLine($"Expected: {item.ToString()}");

        // Act
        var actual = await _tasksRepository.GetUserTasksAsync(userId);
        foreach (var item in actual)
            _output.WriteLine($"Expected: {item.ToString()}");

        // Assert
        actual.Should().NotBeNull();
        actual.Count.Should().Be(expected.Length);
        actual.Should().BeEquivalentTo(expected);
    }
    
    [Fact]
    public async Task GetUserTasks_NoTasks_ShouldSucceed()
    {
        // Arrange
        var userId = Guid.NewGuid();
        
        var initalData = _fixture.CreateMany<AppTask>(10).ToArray();
        
        _dbContext.Tasks.AddRange(initalData);
        _dbContext.SaveChanges();
        
        
        _output.WriteLine($"userId: {userId.ToString()}");

        // Act
        var actual = await _tasksRepository.GetUserTasksAsync(userId);

        // Assert
        actual.Should().NotBeNull();
        actual.Count.Should().Be(0);
    }

    #endregion
}