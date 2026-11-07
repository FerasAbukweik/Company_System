namespace HR_System.Core.DTO.Notification;

public class NotificationPageDTO
{
    public required IReadOnlyList<NotificationDTO> Items { get; set; }
    public required int UnreadCount { get; set; }
}
