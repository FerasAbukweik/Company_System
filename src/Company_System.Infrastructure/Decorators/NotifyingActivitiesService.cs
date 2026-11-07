using HR_System.Core.Abstraction.RepositoryContracts;
using HR_System.Core.Abstraction.ServiceContracts;
using HR_System.Core.DTO.Activity;
using HR_System.Core.DTO.LazyLoading;
using HR_System.Core.DTO.Notification;
using HR_System.Core.Enums;
using HR_System.Core.Result;

namespace HR_System.Infrastructure.Decorators;

public class NotifyingActivitiesService(
    IActivitiesService inner,
    IActivityRepository repository,
    INotificationsService notifications) : IActivitiesService
{
    public async Task<Result<ActivityDto>> AddAsync(ActivityAddDto data, Guid triggeredById, CancellationToken cancellationToken = default)
    {
        var result = await inner.AddAsync(data, triggeredById, cancellationToken);
        if (!result.IsSuccess) return result;

        var activity = await repository.GetByIdAsync(result.Value!.Id, [item => item.Task, item => item.Approval], cancellationToken);
        if (activity is null) return Result<ActivityDto>.Failure("Activity not found after saving");

        var recipient = data.Type switch
        {
            ActivityTypeEnum.TaskAdded => activity.Task?.UserId,
            ActivityTypeEnum.ApprovalPending => activity.Approval?.ManagerId,
            ActivityTypeEnum.ApprovalApproved or ActivityTypeEnum.ApprovalRejected => activity.Approval?.UserRequestingId,
            _ => null
        };
        if (!recipient.HasValue || recipient == triggeredById) return result;

        var type = data.Type switch
        {
            ActivityTypeEnum.TaskAdded => NotificationTypeEnum.TaskAssigned,
            ActivityTypeEnum.ApprovalPending => NotificationTypeEnum.ApprovalRequested,
            _ => NotificationTypeEnum.ApprovalDecision
        };
        var notification = await notifications.AddAsync(new NotificationAddDTO
        {
            UserId = recipient.Value,
            Title = result.Value.Title,
            Content = result.Value.Description,
            TargetPath = "/dashboard",
            SourceKey = $"activity:{result.Value.Id}",
            Type = type
        }, cancellationToken);

        return notification.IsSuccess ? result : notification.MapFailure<ActivityDto>();
    }

    public Task<Result<IReadOnlyList<ActivityDto>>> LazyGetAllSortedAsync(LazyDTO lazyData, Guid userId, CancellationToken cancellationToken = default)
    {
        return inner.LazyGetAllSortedAsync(lazyData, userId, cancellationToken);
    }
}
