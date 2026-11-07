using System.ComponentModel.DataAnnotations;
using HR_System.Core.Domain.Identity;
using HR_System.Core.DTO.Notification;
using HR_System.Core.Enums;

namespace HR_System.Core.Domain.Entities;

public class Notification
{
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required, MaxLength(120)]
    public required string Title { get; set; }

    [Required, MaxLength(1000)]
    public required string Content { get; set; }

    [Required, MaxLength(200)]
    public required string TargetPath { get; set; }

    [Required, MaxLength(150)]
    public required string SourceKey { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public bool IsRead { get; set; }

    public required NotificationTypeEnum Type { get; set; }


    // relations

    public required Guid UserId { get; set; }

    public ApplicationUser? User { get; set; }


    // functions

    public NotificationDTO ToDTO()
    {
        return new NotificationDTO
        {
            Id = Id,
            UserId = UserId,
            Title = Title,
            Content = Content,
            TargetPath = TargetPath,
            CreatedAt = DateTime.SpecifyKind(CreatedAt, DateTimeKind.Utc),
            IsRead = IsRead,
            Type = Type
        };
    }
}
