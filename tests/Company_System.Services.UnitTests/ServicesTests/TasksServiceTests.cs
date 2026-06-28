using AutoFixture;
using FluentAssertions;
using HR_System.Core.Domain.Entities;
using HR_System.Core.DTO.Task;
using HR_System.Core.Enums;
using HR_System.Core.Interfaces.RepositoryContracts;
using HR_System.Core.Interfaces.ServiceContracts.ITaskServices;
using HR_System.Infrastructure.Services;
using Moq;
using Xunit.Abstractions;

namespace HR_System.Core.UnitTests.ServicesTests;

public class TasksServiceTests
{
    private readonly ITasksService _tasksService;
    private readonly ITestOutputHelper _output;
    private readonly IFixture _fixture;
    private readonly Mock<ITasksRepository> _tasksRepository;

    public TasksServiceTests(ITestOutputHelper output)
    {
        _output = output;
        
        _fixture = new Fixture();
        _fixture.Behaviors.OfType<ThrowingRecursionBehavior>().ToList()
            .ForEach(b => _fixture.Behaviors.Remove(b));
        _fixture.Behaviors.Add(new OmitOnRecursionBehavior());

        _tasksRepository = new Mock<ITasksRepository>();

        _tasksService = new TasksesService(_tasksRepository.Object);
    }

    #region GetUserTasksTests

    [Fact]
    public async Task GetUserTasksTests_ValidData_ShouldSucceed()
    {
        // Arrange
        var userId = Guid.NewGuid();
        
        var userTasks = _fixture.CreateMany<AppTask>(4).ToArray();
        for (int i = 0; i < userTasks.Length; i++)
            userTasks[i].UserId = userId;
        
        _tasksRepository.Setup(t => t.GetUserTasksAsync(It.IsAny<Guid>()))
            .ReturnsAsync(userTasks);

        var expected = userTasks.Select(t => t.ToDTO()).ToArray();
        
        foreach (var item in expected)
            _output.WriteLine($"Expected: {item.ToString()}\n");
        
        // Act
        var actual = await  _tasksService.GetUserTasksAsync(userId);
        foreach (var item in actual)
            _output.WriteLine($"Actual: {item.ToString()}\n");
        // Assert
        actual.Should().NotBeNull();
        actual.Count.Should().Be(expected.Length);
        actual.Should().BeEquivalentTo(expected);
    }
    

    #endregion
    
    #region SetTests

    [Fact]
    public async Task SetTests_ValidData_ShouldSucceed()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var toAddTask =  _fixture.Create<TaskAddDTO>();
        
        _tasksRepository.Setup(t => t.SaveChangesAsync())
            .ReturnsAsync(true);

        _output.WriteLine($"userId: {userId}\nToAddTask: {toAddTask.ToString()}");
        
        
        // Act
        var actual = await _tasksService.AddAsync(toAddTask, userId);
        _output.WriteLine($"Actual: {actual.ToString()}");
        
        // Assert
        actual.Should().NotBeNull();
        actual.IsSuccess.Should().BeTrue();
        actual.Value.Should().NotBeNull();
        actual.Value.UserId.Should().Be(toAddTask.UserId);
        actual.Value.Deadline.Should().Be(toAddTask.Deadline);
        actual.Value.Title.Should().Be(toAddTask.Title);
        actual.Value.Description.Should().Be(toAddTask.Description);
        actual.Value.Priority.Should().Be(toAddTask.Priority);
    }
    
    [Fact]
    public async Task SetTests_FailedToSaveToDB_ShouldSucceed()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var toAddTask =  _fixture.Create<TaskAddDTO>();
        
        _tasksRepository.Setup(t => t.SaveChangesAsync())
            .ReturnsAsync(false);

        _output.WriteLine($"userId: {userId}\nToAddTask: {toAddTask.ToString()}");
        
        
        // Act
        var actual = await _tasksService.AddAsync(toAddTask, userId);
        _output.WriteLine($"Actual: {actual.ToString()}");
        
        // Assert
        actual.Should().NotBeNull();
        actual.IsSuccess.Should().BeFalse();
        actual.Value.Should().BeNull();
    }

    #endregion

    #region UpdateStatusTests

    [Fact]
    public async Task UpdateStatus_ValidData_ShouldSucceed()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var taskId = Guid.NewGuid();
        var newStatus = _fixture.Create<TaskStatusEnum>();

        var updatedTask = _fixture.Create<AppTask>();
        updatedTask.UserId = userId;
        updatedTask.Status = newStatus;

        _tasksRepository.Setup(t => t.UpdateStatusAsync(It.IsAny<Guid>(), It.IsAny<TaskStatusEnum>()))
            .ReturnsAsync(updatedTask);
        
        _tasksRepository.Setup(t => t.SaveChangesAsync())
            .ReturnsAsync(true);
        
        var expected = updatedTask.ToDTO();
        
        _output.WriteLine($"Expected: {expected.ToString()}");

        // Act
        var actual = await _tasksService.UpdateStatusAsync(userId, taskId, newStatus);
        _output.WriteLine($"Actual: {actual.Value?.ToString() ?? "null"}");

        // Assert
        actual.Should().NotBeNull();
        actual.IsSuccess.Should().BeTrue();
        actual.Value.Should().BeEquivalentTo(expected);
    }
    
    
    [Fact]
    public async Task UpdateStatus_FailedToSaveChanges_ShouldSucceed()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var taskId = Guid.NewGuid();
        var newStatus = _fixture.Create<TaskStatusEnum>();

        var updatedTask = _fixture.Create<AppTask>();
        updatedTask.UserId = userId;
        updatedTask.Status = newStatus;

        _tasksRepository.Setup(t => t.UpdateStatusAsync(It.IsAny<Guid>(), It.IsAny<TaskStatusEnum>()))
            .ReturnsAsync(updatedTask);
        
        _tasksRepository.Setup(t => t.SaveChangesAsync())
            .ReturnsAsync(false);
        
        var expected = updatedTask.ToDTO();
        
        _output.WriteLine($"Expected: {expected.ToString()}");

        // Act
        var actual = await _tasksService.UpdateStatusAsync(userId, taskId, newStatus);
        _output.WriteLine($"Actual: {actual.Value?.ToString() ?? "null"}");

        // Assert
        actual.Should().NotBeNull();
        actual.IsSuccess.Should().BeFalse();
        actual.Value.Should().BeNull();
    }
    
    
    [Fact]
    public async Task UpdateStatus_NoTaskToUpdate_ShouldSucceed()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var taskId = Guid.NewGuid();
        var newStatus = _fixture.Create<TaskStatusEnum>();

        var updatedTask = _fixture.Create<AppTask>();
        updatedTask.UserId = userId;
        updatedTask.Status = newStatus;

        _tasksRepository.Setup(t => t.UpdateStatusAsync(It.IsAny<Guid>(), It.IsAny<TaskStatusEnum>()))
            .ReturnsAsync(null as AppTask);
        
        var expected = updatedTask.ToDTO();
        
        _output.WriteLine($"Expected: {expected.ToString()}");

        // Act
        var actual = await _tasksService.UpdateStatusAsync(userId, taskId, newStatus);
        _output.WriteLine($"Actual: {actual.Value?.ToString() ?? "null"}");

        // Assert
        actual.Should().NotBeNull();
        actual.IsSuccess.Should().BeFalse();
        actual.Value.Should().BeNull();
    }

    #endregion
    
}