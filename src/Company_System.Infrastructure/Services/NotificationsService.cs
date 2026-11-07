using System.Net;
using HR_System.Core.Abstraction.RepositoryContracts;
using HR_System.Core.Abstraction.ServiceContracts;
using HR_System.Core.Domain.Entities;
using HR_System.Core.DTO.LazyLoading;
using HR_System.Core.DTO.Notification;
using HR_System.Core.Result;

namespace HR_System.Infrastructure.Services;

public class NotificationsService(INotificationsRepository repository, INotificationPublisher publisher) : INotificationsService
{
    public async Task<Result<NotificationDTO>> AddAsync(NotificationAddDTO data, CancellationToken cancellationToken = default)
    {
        if (data.UserId == Guid.Empty || string.IsNullOrWhiteSpace(data.Title) || data.Title.Length > 120 ||
            string.IsNullOrWhiteSpace(data.Content) || data.Content.Length > 1000 ||
            string.IsNullOrWhiteSpace(data.SourceKey) || data.SourceKey.Length > 150 ||
            string.IsNullOrWhiteSpace(data.TargetPath) || data.TargetPath.Length > 200 ||
            !data.TargetPath.StartsWith('/') || data.TargetPath.StartsWith("//") || !Enum.IsDefined(data.Type))
            return Result<NotificationDTO>.Failure("Invalid notification", HttpStatusCode.BadRequest);

        var existing = await repository.FilterAsync(
            notification => notification.UserId == data.UserId && notification.SourceKey == data.SourceKey,
            take: 1, cancellationToken: cancellationToken);
        if (existing.Count > 0) return Result<NotificationDTO>.Success(existing[0].ToDTO());

        var notification = new Notification
        {
            UserId = data.UserId,
            Title = data.Title,
            Content = data.Content,
            TargetPath = data.TargetPath,
            SourceKey = data.SourceKey,
            Type = data.Type
        };
        repository.Add(notification, cancellationToken);

        if (!await repository.SaveChangesAsync(cancellationToken))
            return Result<NotificationDTO>.Failure("Failed to save notification");

        var dto = notification.ToDTO();
        await publisher.PublishAsync(dto, cancellationToken);

        return Result<NotificationDTO>.Success(dto, HttpStatusCode.Created);
    }

    public async Task<Result<NotificationPageDTO>> GetAsync(Guid userId, LazyDTO lazyData, CancellationToken cancellationToken = default)
    {
        if (userId == Guid.Empty || lazyData.Taken < 0 || lazyData.SectionSize < 1)
            return Result<NotificationPageDTO>.Failure("Invalid notification page", HttpStatusCode.BadRequest);

        var notifications = await repository.FilterAsync(notification => notification.UserId == userId,
            orderBy: notification => notification.CreatedAt, orderByDesc: true,
            skip: lazyData.Taken, take: lazyData.SectionSize, cancellationToken: cancellationToken);
        var unread = await repository.GetUnreadCountAsync(userId, cancellationToken);

        return Result<NotificationPageDTO>.Success(new NotificationPageDTO
        {
            Items = notifications.Select(notification => notification.ToDTO()).ToList(),
            UnreadCount = unread
        });
    }

    public async Task<Result<int>> GetUnreadCountAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        return Result<int>.Success(await repository.GetUnreadCountAsync(userId, cancellationToken));
    }

    public async Task<Result<bool>> MarkAllReadAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var notifications = await repository.FilterAsync(notification => notification.UserId == userId && !notification.IsRead,
            cancellationToken: cancellationToken);
        if (notifications.Count == 0) return Result<bool>.Success(true);

        foreach (var notification in notifications) repository.SetRead(notification);

        return await repository.SaveChangesAsync(cancellationToken)
            ? Result<bool>.Success(true)
            : Result<bool>.Failure("Failed to mark notifications as read");
    }
}
