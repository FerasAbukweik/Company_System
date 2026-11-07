using HR_System.Core.DTO.Notification;

namespace HR_System.Core.Abstraction.ServiceContracts;

public interface INotificationPublisher
{
    Task PublishAsync(NotificationDTO notification, CancellationToken cancellationToken = default);

    Task FlushAsync();

    void Clear();
}
