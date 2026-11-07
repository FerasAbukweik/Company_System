using HR_System.Core.DTO.Message;
using HR_System.Core.DTO.Notification;

namespace HR_System.SignalR.App;

public interface IAppClient
{
    Task ReceiveNotification(NotificationDTO notification);
    Task ReceiveMessage(MessageDTO message);
    Task NotifyTyping(Guid userId);
    Task NotifyStoppedTyping(Guid userId);
}
