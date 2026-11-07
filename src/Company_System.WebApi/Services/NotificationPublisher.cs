using HR_System.Core.Abstraction.ServiceContracts;
using HR_System.Core.DTO.Notification;
using HR_System.Infrastructure;
using HR_System.SignalR.App;
using Microsoft.AspNetCore.SignalR;

namespace HR_System.Services;

public class NotificationPublisher(
    IHubContext<AppHub, IAppClient> hub,
    ApplicationDbContext dbContext,
    ILogger<NotificationPublisher> logger) : INotificationPublisher
{
    private readonly List<NotificationDTO> pending = [];

    public Task PublishAsync(NotificationDTO notification, CancellationToken cancellationToken = default)
    {
        if (dbContext.Database.CurrentTransaction is not null)
        {
            pending.Add(notification);
            return Task.CompletedTask;
        }

        return SendAsync(notification);
    }

    public async Task PublishPendingAsync()
    {
        foreach (var notification in pending) await SendAsync(notification);
        pending.Clear();
    }

    public void DiscardPending() => pending.Clear();

    private async Task SendAsync(NotificationDTO notification)
    {
        try
        {
            await hub.Clients.User(notification.UserId.ToString()).ReceiveNotification(notification);
        }
        catch (Exception exception)
        {
            // The committed notification remains available through the API if live delivery fails.
            logger.LogWarning(exception, "Live delivery failed for notification {NotificationId}", notification.Id);
        }
    }
}
