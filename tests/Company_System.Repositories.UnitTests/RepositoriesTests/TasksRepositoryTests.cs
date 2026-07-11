using AutoFixture;
   using FluentAssertions;
   using HR_System.Core.Domain.Entities;
   using HR_System.Core.Domain.Identity;
   using HR_System.Core.DTO.LazyLoading;
   using HR_System.Core.Enums;
   using HR_System.Core.Interfaces.RepositoryContracts;
   using HR_System.Core.Interfaces.ServiceContracts;
   using HR_System.Infrastructure;
   using HR_System.Infrastructure.Repositories;
   using Microsoft.EntityFrameworkCore;
   using Moq;
   using Xunit.Abstractions;
   
   namespace TestProject1.RepositoriesTests;
   
   public class TasksRepositoryTests : IDisposable
   {
       private readonly ITasksRepository _tasksRepository;
       private readonly ApplicationDbContext _dbContext;
       private readonly Mock<IRedisService> _cacheMock;
       private readonly ITestOutputHelper _output;
       private readonly IFixture _fixture;
   
       public TasksRepositoryTests(ITestOutputHelper output)
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
   
           // IRedisService is an external dependency (cache), not part of this
           // repository's own logic, so it's mocked rather than exercised for real.
           _cacheMock = new Mock<IRedisService>();
   
           _tasksRepository = new TasksRepository(_dbContext, _cacheMock.Object);
       }
   
       private AppTask CreateTask(
           Guid? userId = null,
           Guid? managerId = null,
           TaskStatusEnum? status = null,
           DateTime? createdAt = null)
       {
           return _fixture.Build<AppTask>()
               .With(t => t.UserId, userId ?? Guid.NewGuid())
               .With(t => t.ManagerId, managerId ?? Guid.NewGuid())
               .With(t => t.Status, status ?? TaskStatusEnum.Pending)
               .With(t => t.CreatedAt, createdAt ?? DateTime.UtcNow)
               .Without(t => t.User)
               .Without(t => t.Manager)
               .Without(t => t.Approvals)
               .Create();
       }
   
       private ApplicationUser CreateUser(string? userName = null)
       {
           return _fixture.Build<ApplicationUser>()
               .With(u => u.UserName, userName ?? $"user_{Guid.NewGuid():N}")
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
   
       #region Add
   
       [Fact]
       public void Add_ShouldTrackEntityAsAdded()
       {
           // Arrange
           var task = CreateTask();
   
           // Act
           _tasksRepository.Add(task);
   
           // Assert
           _dbContext.Entry(task).State.Should().Be(EntityState.Added);
           _dbContext.Tasks.Local.Should().Contain(task);
       }
   
       [Fact]
       public void Add_ShouldNotPersistToDatabase_BeforeSaveChangesIsCalled()
       {
           // Arrange
           var task = CreateTask();
   
           // Act
           _tasksRepository.Add(task);
   
           // Assert
           _dbContext.Tasks.AsNoTracking().Any(t => t.Id == task.Id).Should().BeFalse();
       }
   
       #endregion
   
       #region LazyGetUserTasksAsync
   
       [Fact]
       public async Task LazyGetUserTasksAsync_ShouldReturnOnlyTasksForGivenUser()
       {
           // Arrange
           var userId = Guid.NewGuid();
           var otherUserId = Guid.NewGuid();
   
           var userTask = CreateTask(userId: userId);
           var otherUserTask = CreateTask(userId: otherUserId);
   
           _dbContext.Tasks.AddRange(userTask, otherUserTask);
           await _dbContext.SaveChangesAsync();
   
           var lazyData = new LazyDTO { Taken = 0, SectionSize = 10 };
   
           // Act
           var result = await _tasksRepository.LazyGetUserTasksAsync(userId, lazyData);
   
           // Assert
           result.Should().ContainSingle(t => t.Id == userTask.Id);
           result.Should().NotContain(t => t.Id == otherUserTask.Id);
       }
   
       [Fact]
       public async Task LazyGetUserTasksAsync_ShouldExcludeCompletedTasks()
       {
           // Arrange
           var userId = Guid.NewGuid();
   
           var pendingTask = CreateTask(userId: userId, status: TaskStatusEnum.Pending);
           var completedTask = CreateTask(userId: userId, status: TaskStatusEnum.Completed);
   
           _dbContext.Tasks.AddRange(pendingTask, completedTask);
           await _dbContext.SaveChangesAsync();
   
           var lazyData = new LazyDTO { Taken = 0, SectionSize = 10 };
   
           // Act
           var result = await _tasksRepository.LazyGetUserTasksAsync(userId, lazyData);
   
           // Assert
           result.Should().ContainSingle(t => t.Id == pendingTask.Id);
           result.Should().NotContain(t => t.Id == completedTask.Id);
       }
   
       [Fact]
       public async Task LazyGetUserTasksAsync_ShouldReturnTasksOrderedByCreatedAtDescending()
       {
           // Arrange
           var userId = Guid.NewGuid();
   
           var oldest = CreateTask(userId: userId, createdAt: DateTime.UtcNow.AddDays(-2));
           var middle = CreateTask(userId: userId, createdAt: DateTime.UtcNow.AddDays(-1));
           var newest = CreateTask(userId: userId, createdAt: DateTime.UtcNow);
   
           _dbContext.Tasks.AddRange(oldest, middle, newest);
           await _dbContext.SaveChangesAsync();
   
           var lazyData = new LazyDTO { Taken = 0, SectionSize = 10 };
   
           // Act
           var result = await _tasksRepository.LazyGetUserTasksAsync(userId, lazyData);
   
           // Assert
           result.Select(t => t.Id).Should().ContainInOrder(newest.Id, middle.Id, oldest.Id);
       }
   
       [Fact]
       public async Task LazyGetUserTasksAsync_ShouldRespectSkipAndTake()
       {
           // Arrange
           var userId = Guid.NewGuid();
   
           var tasks = Enumerable.Range(0, 5)
               .Select(i => CreateTask(userId: userId, createdAt: DateTime.UtcNow.AddMinutes(-i)))
               .ToList();
   
           _dbContext.Tasks.AddRange(tasks);
           await _dbContext.SaveChangesAsync();
   
           var lazyData = new LazyDTO { Taken = 1, SectionSize = 2 };
   
           // Act
           var result = await _tasksRepository.LazyGetUserTasksAsync(userId, lazyData);
   
           // Assert — sorted desc by CreatedAt: [0,1,2,3,4] -> skip 1, take 2 -> [1,2]
           result.Should().HaveCount(2);
           result.Select(t => t.Id).Should().ContainInOrder(tasks[1].Id, tasks[2].Id);
       }
   
       [Fact]
       public async Task LazyGetUserTasksAsync_ShouldReturnEmptyList_WhenUserHasNoMatchingTasks()
       {
           // Arrange
           var lazyData = new LazyDTO { Taken = 0, SectionSize = 10 };
   
           // Act
           var result = await _tasksRepository.LazyGetUserTasksAsync(Guid.NewGuid(), lazyData);
   
           // Assert
           result.Should().BeEmpty();
       }
   
       #endregion
   
       #region UpdateStatusAsync
   
       [Fact]
       public async Task UpdateStatusAsync_ShouldUpdateStatus_WhenTaskExists()
       {
           // Arrange
           var task = CreateTask(status: TaskStatusEnum.Pending);
           _dbContext.Tasks.Add(task);
           await _dbContext.SaveChangesAsync();
   
           // Act
           var result = await _tasksRepository.UpdateStatusAsync(task.Id, TaskStatusEnum.Completed);
   
           // Assert
           result.Should().NotBeNull();
           result!.Status.Should().Be(TaskStatusEnum.Completed);
       }
   
       [Fact]
       public async Task UpdateStatusAsync_ShouldPersistUpdatedStatus_AfterSaveChanges()
       {
           // Arrange
           var task = CreateTask(status: TaskStatusEnum.Pending);
           _dbContext.Tasks.Add(task);
           await _dbContext.SaveChangesAsync();
   
           // Act
           await _tasksRepository.UpdateStatusAsync(task.Id, TaskStatusEnum.Completed);
           await _dbContext.SaveChangesAsync();
           _dbContext.ChangeTracker.Clear();
   
           // Assert
           var fromDb = await _dbContext.Tasks.SingleAsync(t => t.Id == task.Id);
           fromDb.Status.Should().Be(TaskStatusEnum.Completed);
       }
   
       [Fact]
       public async Task UpdateStatusAsync_ShouldReturnNull_WhenTaskDoesNotExist()
       {
           // Act
           var result = await _tasksRepository.UpdateStatusAsync(Guid.NewGuid(), TaskStatusEnum.Completed);
   
           // Assert
           result.Should().BeNull();
       }
   
       #endregion
   
       #region GetTaskAsync
   
       [Fact]
       public async Task GetTaskAsync_ShouldReturnCachedTask_WhenPresentInCache()
       {
           // Arrange
           var task = CreateTask();
           var cacheKey = $"Task-Id-{task.Id}";
   
           _cacheMock
               .Setup(c => c.GetAsync<AppTask>(cacheKey, It.IsAny<CancellationToken>()))
               .ReturnsAsync(task);
   
           // Act
           var result = await _tasksRepository.GetTaskAsync(task.Id);
   
           // Assert
           result.Should().Be(task);
           _cacheMock.Verify(
               c => c.SetAsync(It.IsAny<string>(), It.IsAny<AppTask?>(), It.IsAny<CancellationToken>()),
               Times.Never);
       }
       
   
       [Fact]
       public async Task GetTaskAsync_ShouldIncludeUserNavigationProperty()
       {
           // Arrange
           var user = CreateUser("task_owner");
           _dbContext.Users.Add(user);
   
           var task = CreateTask(userId: user.Id);
           _dbContext.Tasks.Add(task);
           await _dbContext.SaveChangesAsync();
           _dbContext.ChangeTracker.Clear();
   
           var cacheKey = $"Task-Id-{task.Id}";
           _cacheMock
               .Setup(c => c.GetAsync<AppTask>(cacheKey, It.IsAny<CancellationToken>()))
               .ReturnsAsync((AppTask?)null);
   
           // Act
           var result = await _tasksRepository.GetTaskAsync(task.Id);
   
           // Assert
           result.Should().NotBeNull();
           result!.User.Should().NotBeNull();
           result.User!.UserName.Should().Be("task_owner");
       }
   
       [Fact]
       public async Task GetTaskAsync_ShouldReturnNull_WhenTaskNotInCacheOrDb()
       {
           // Arrange
           var taskId = Guid.NewGuid();
           var cacheKey = $"Task-Id-{taskId}";
   
           _cacheMock
               .Setup(c => c.GetAsync<AppTask>(cacheKey, It.IsAny<CancellationToken>()))
               .ReturnsAsync((AppTask?)null);
   
           // Act
           var result = await _tasksRepository.GetTaskAsync(taskId);
   
           // Assert
           result.Should().BeNull();
           _cacheMock.Verify(
               c => c.SetAsync(It.IsAny<string>(), It.IsAny<AppTask?>(), It.IsAny<CancellationToken>()),
               Times.Never);
       }
   
       #endregion
   
       #region RemoveAsync
   
       [Fact]
       public async Task RemoveAsync_ShouldMarkTaskAsRemovedAndClearCache_WhenTaskExists()
       {
           // Arrange
           var task = CreateTask();
           _dbContext.Tasks.Add(task);
           await _dbContext.SaveChangesAsync();
           _dbContext.ChangeTracker.Clear();
   
           // Act
           var result = await _tasksRepository.RemoveAsync(task.Id);
   
           // Assert
           result.Should().NotBeNull();
           result!.Id.Should().Be(task.Id);
           _dbContext.Entry(result).State.Should().Be(EntityState.Deleted);
   
           _cacheMock.Verify(c => c.RemoveAsync($"Task-Id-{task.Id}", It.IsAny<CancellationToken>()), Times.Once);
       }
   
       [Fact]
       public async Task RemoveAsync_ShouldPersistRemoval_AfterSaveChanges()
       {
           // Arrange
           var task = CreateTask();
           _dbContext.Tasks.Add(task);
           await _dbContext.SaveChangesAsync();
           _dbContext.ChangeTracker.Clear();
   
           // Act
           await _tasksRepository.RemoveAsync(task.Id);
           await _dbContext.SaveChangesAsync();
   
           // Assert
           (await _dbContext.Tasks.AnyAsync(t => t.Id == task.Id)).Should().BeFalse();
       }
   
       [Fact]
       public async Task RemoveAsync_ShouldReturnNull_WhenTaskDoesNotExist()
       {
           // Act
           var result = await _tasksRepository.RemoveAsync(Guid.NewGuid());
   
           // Assert
           result.Should().BeNull();
           _cacheMock.Verify(c => c.RemoveAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
       }
   
       #endregion
   
       #region SaveChangesAsync
   
       [Fact]
       public async Task SaveChangesAsync_ShouldReturnTrue_WhenThereArePendingChanges()
       {
           // Arrange
           _dbContext.Tasks.Add(CreateTask());
   
           // Act
           var result = await _tasksRepository.SaveChangesAsync();
   
           // Assert
           result.Should().BeTrue();
       }
   
       [Fact]
       public async Task SaveChangesAsync_ShouldReturnFalse_WhenThereAreNoPendingChanges()
       {
           // Act
           var result = await _tasksRepository.SaveChangesAsync();
   
           // Assert
           result.Should().BeFalse();
       }
   
       #endregion
   
       public void Dispose() => _dbContext.Dispose();
   }