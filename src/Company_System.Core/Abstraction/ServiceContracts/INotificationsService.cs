using HR_System.Core.DTO.LazyLoading;
using HR_System.Core.DTO.Notification;
using HR_System.Core.Result;

namespace HR_System.Core.Abstraction.ServiceContracts;

public interface INotificationsService
{
    Task<Result<NotificationDTO>> AddAsync(NotificationAddDTO data, CancellationToken cancellationToken = default);

    Task<Result<NotificationPageDTO>> GetAsync(Guid userId, LazyDTO lazyData, CancellationToken cancellationToken = default);

    Task<Result<int>> GetUnreadCountAsync(Guid userId, CancellationToken cancellationToken = default);

    Task<Result<bool>> MarkAllReadAsync(Guid userId, CancellationToken cancellationToken = default);
}
