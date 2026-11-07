using HR_System.Core.Abstraction.ServiceContracts;
using HR_System.Core.DTO.LazyLoading;
using HR_System.Core.DTO.Notification;
using HR_System.Core.Result;
using HR_System.Infrastructure.Services;
using Microsoft.Extensions.Logging;

namespace HR_System.Infrastructure.Decorators.Loggers;

public class LoggingNotificationsService(INotificationsService inner, ILogger<LoggingNotificationsService> logger) : INotificationsService
{
    public async Task<Result<NotificationDTO>> AddAsync(NotificationAddDTO data, CancellationToken cancellationToken = default)
    {
        var result = await inner.AddAsync(data, cancellationToken);
        logger.LogResult(result, nameof(NotificationsService), nameof(AddAsync), $"UserId: {data.UserId}");

        return result;
    }

    public async Task<Result<NotificationPageDTO>> GetAsync(Guid userId, LazyDTO lazyData, CancellationToken cancellationToken = default)
    {
        var result = await inner.GetAsync(userId, lazyData, cancellationToken);
        logger.LogResult(result, nameof(NotificationsService), nameof(GetAsync), $"UserId: {userId}");

        return result;
    }

    public async Task<Result<int>> GetUnreadCountAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var result = await inner.GetUnreadCountAsync(userId, cancellationToken);
        logger.LogResult(result, nameof(NotificationsService), nameof(GetUnreadCountAsync), $"UserId: {userId}");

        return result;
    }

    public async Task<Result<bool>> MarkAllReadAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var result = await inner.MarkAllReadAsync(userId, cancellationToken);
        logger.LogResult(result, nameof(NotificationsService), nameof(MarkAllReadAsync), $"UserId: {userId}");

        return result;
    }

}
