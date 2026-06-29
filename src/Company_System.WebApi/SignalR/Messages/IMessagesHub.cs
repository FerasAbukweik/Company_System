using HR_System.Core.DTO.Message;

namespace HR_System.SignalR.Messages;

public interface IMessagesHub
{
    Task NotifyTyping();
    Task NotifyStoppedTyping();
    Task ReceiveMessage(MessageDTO message);
}