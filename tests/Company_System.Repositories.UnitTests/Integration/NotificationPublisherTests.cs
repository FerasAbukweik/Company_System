using HR_System.Core.DTO.Notification;
using HR_System.Core.Enums;
using HR_System.Infrastructure;
using HR_System.Services;
using HR_System.SignalR.App;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace TestProject1.Integration;

public class NotificationPublisherTests : RepositoryTestBase
{
    public NotificationPublisherTests(ITestOutputHelper output) : base(output) { }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task Publish_ShouldOnlyDeliverAfterACommittedTransaction(bool commit)
    {
        // Arrange
        await using var context = new ApplicationDbContext(new DbContextOptionsBuilder<ApplicationDbContext>().UseSqlite("Data Source=:memory:").Options);
        await context.Database.OpenConnectionAsync();
        var client = new Mock<IAppClient>();
        var clients = new Mock<IHubClients<IAppClient>>();
        clients.Setup(hub => hub.User(It.IsAny<string>())).Returns(client.Object);
        var hub = new Mock<IHubContext<AppHub, IAppClient>>();
        hub.SetupGet(value => value.Clients).Returns(clients.Object);
        var publisher = new NotificationPublisher(hub.Object, context, NullLogger<NotificationPublisher>.Instance);
        var notification = new NotificationDTO
        {
            Id = Guid.NewGuid(), UserId = Guid.NewGuid(), Title = "New task", Content = "Assigned", TargetPath = "/dashboard",
            CreatedAt = DateTime.UtcNow, IsRead = false, Type = NotificationTypeEnum.TaskAssigned
        };
        await using var transaction = await context.Database.BeginTransactionAsync();
        LogArrange(nameof(commit), commit);

        // Act
        await publisher.PublishAsync(notification);
        client.VerifyNoOtherCalls();
        if (commit) await transaction.CommitAsync();
        else
        {
            await transaction.RollbackAsync();
            publisher.DiscardPending();
        }
        await publisher.PublishPendingAsync();
        LogAct("Pending notifications flushed");

        // Assert
        client.Verify(value => value.ReceiveNotification(notification), commit ? Times.Once : Times.Never);
        if (commit) clients.Verify(value => value.User(notification.UserId.ToString()), Times.Once);
    }
    [Fact]
    public async Task Publish_ShouldSendImmediatelyWithoutATransaction()
    {
        // Arrange
        await using var context = new ApplicationDbContext(new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlite("Data Source=:memory:").Options);
        var client = new Mock<IAppClient>();
        var clients = new Mock<IHubClients<IAppClient>>();
        clients.Setup(value => value.User(It.IsAny<string>())).Returns(client.Object);
        var hub = new Mock<IHubContext<AppHub, IAppClient>>();
        hub.SetupGet(value => value.Clients).Returns(clients.Object);
        var publisher = new NotificationPublisher(hub.Object, context, NullLogger<NotificationPublisher>.Instance);
        var notification = new NotificationDTO
        {
            Id = Guid.NewGuid(), UserId = Guid.NewGuid(), Title = "New task", Content = "Assigned", TargetPath = "/dashboard",
            CreatedAt = DateTime.UtcNow, IsRead = false, Type = NotificationTypeEnum.TaskAssigned
        };
        LogArrange(nameof(notification), notification);

        // Act
        await publisher.PublishAsync(notification);
        await publisher.PublishPendingAsync();
        LogAct("Published outside a transaction");

        // Assert
        client.Verify(value => value.ReceiveNotification(notification), Times.Once);
    }

}
