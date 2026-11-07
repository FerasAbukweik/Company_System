using HR_System.Core.Abstraction.ServiceContracts;
using HR_System.Core.Domain.Entities;
using HR_System.Core.DTO.LazyLoading;
using HR_System.Core.DTO.Notification;
using HR_System.Core.Enums;
using HR_System.Infrastructure;
using HR_System.Infrastructure.Repositories;
using HR_System.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Moq;

namespace HR_System.Core.UnitTests.Integration;

public class NotificationsTests : ServiceTestBase, IDisposable
{
    private readonly ApplicationDbContext _context = new(new DbContextOptionsBuilder<ApplicationDbContext>()
        .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);
    private readonly Mock<INotificationPublisher> _publisher = new();
    private readonly NotificationsService _service;

    public NotificationsTests(ITestOutputHelper output) : base(output)
    {
        _service = new NotificationsService(new NotificationsRepository(_context), _publisher.Object);
    }

    private static NotificationAddDTO CreateData(Guid userId) => new()
    {
        UserId = userId,
        Title = "Task assigned",
        Content = "A new task is ready",
        TargetPath = "/dashboard",
        SourceKey = Guid.NewGuid().ToString(),
        Type = NotificationTypeEnum.TaskAssigned
    };

    [Fact]
    public async Task Add_ShouldPersistAndPublishOnlyOnceForTheSameEvent()
    {
        // Arrange
        var data = CreateData(Guid.NewGuid());
        LogArrange(nameof(data), data);

        // Act
        var first = await _service.AddAsync(data);
        _context.ChangeTracker.Clear();
        var repeated = await _service.AddAsync(data);
        LogAct(repeated);

        // Assert
        first.IsSuccess.Should().BeTrue();
        repeated.Value!.Id.Should().Be(first.Value!.Id);
        (await _context.Notifications.CountAsync()).Should().Be(1);
        _publisher.Verify(publisher => publisher.PublishAsync(It.Is<NotificationDTO>(item => item.Id == first.Value.Id), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Get_ShouldScopeBatchesAndUnreadCountToTheCurrentUser()
    {
        // Arrange
        var userId = Guid.NewGuid();
        await _service.AddAsync(CreateData(userId));
        await _service.AddAsync(CreateData(userId));
        await _service.AddAsync(CreateData(Guid.NewGuid()));
        _context.ChangeTracker.Clear();
        LogArrange(nameof(userId), userId);

        // Act
        var result = await _service.GetAsync(userId, new LazyDTO { Taken = 1, SectionSize = 1 });
        LogAct(result);

        // Assert
        result.Value!.Items.Should().ContainSingle().Which.UserId.Should().Be(userId);
        result.Value.UnreadCount.Should().Be(2);
    }

    [Fact]
    public async Task MarkAllRead_ShouldOnlyChangeTheCurrentUsersNotifications()
    {
        // Arrange
        var userId = Guid.NewGuid();
        await _service.AddAsync(CreateData(userId));
        await _service.AddAsync(CreateData(Guid.NewGuid()));
        _context.ChangeTracker.Clear();
        LogArrange(nameof(userId), userId);

        // Act
        var result = await _service.MarkAllReadAsync(userId);
        LogAct(result);

        // Assert
        result.IsSuccess.Should().BeTrue();
        (await _context.Notifications.SingleAsync(item => item.UserId == userId)).IsRead.Should().BeTrue();
        (await _context.Notifications.SingleAsync(item => item.UserId != userId)).IsRead.Should().BeFalse();
    }

    public void Dispose() => _context.Dispose();
}
