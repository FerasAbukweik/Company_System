using HR_System.Core.Abstraction.RepositoryContracts;
using HR_System.Core.Abstraction.ServiceContracts;
using HR_System.Core.Domain.Entities;
using HR_System.Core.DTO.Activity;
using HR_System.Core.DTO.LazyLoading;
using HR_System.Core.Result;

namespace HR_System.Infrastructure.Services;

public class ActivitiesService(
    IActivityRepository activityRepository,
    IOrganizationHierarchyRepository hierarchyRepository
    ) : IActivitiesService
{
    public async Task<Result<ActivityDto>> AddAsync(ActivityAddDto toAdd, Guid triggeredById, CancellationToken cancellationToken = default)
    {
        var activity = new Activity
        {
            Type = toAdd.Type,
            TriggeredById = triggeredById,
            TaskId = toAdd.TaskId,
            ApprovalId = toAdd.ApprovalId
        };

        activityRepository.Add(activity);

        if (!await activityRepository.SaveChangesAsync(cancellationToken))
        {
            return Result<ActivityDto>.Failure("Failed saving changes to DB");
        }


        var activityWithInclude = await activityRepository.GetByIdAsync(
            activity.Id,
            [
                a => a.TriggeredBy,
                a => a.Task,
                a => a.Approval!.Task,
                a => a.Approval!.Manager,
                a => a.Approval!.UserRequesting
            ],
            cancellationToken);

        return Result<ActivityDto>.Success((activityWithInclude ?? activity).ToDTO());
    }

    public async Task<Result<IReadOnlyList<ActivityDto>>> LazyGetAllSortedAsync(LazyDTO lazyData, Guid userId, CancellationToken cancellationToken = default)
    {
        // users under the current user's subtree
        var subTreeUserIds = await hierarchyRepository.GetSubTreeUserIds(userId, cancellationToken);

        // activities triggered by those users, newest first, one section at a time
        var activities = await activityRepository.FilterAsync(
            a => subTreeUserIds.Contains(a.TriggeredById),
            [
                a => a.TriggeredBy,
                a => a.Task,
                a => a.Approval!.Task,
                a => a.Approval!.Manager,
                a => a.Approval!.UserRequesting
            ],
            a => a.CreatedAt,
            true,
            lazyData.Taken,
            lazyData.SectionSize,
            cancellationToken);

        var result = activities.Select(a => a.ToDTO()).ToList();

        return Result<IReadOnlyList<ActivityDto>>.Success(result);
    }
}
