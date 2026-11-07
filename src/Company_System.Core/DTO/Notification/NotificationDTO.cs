using HR_System.Core.Enums;

namespace HR_System.Core.DTO.Notification;

public class NotificationDTO
{
    public required Guid Id { get; set; }
    public required Guid UserId { get; set; }
    public required string Title { get; set; }
    public required string Content { get; set; }
    public required string TargetPath { get; set; }
    public required DateTime CreatedAt { get; set; }
    public required bool IsRead { get; set; }
    public required NotificationTypeEnum Type { get; set; }
}
