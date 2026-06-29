using HR_System.Core.common;
using HR_System.Core.Domain.Entities;
using HR_System.Core.DTO.Activity;
using HR_System.Core.DTO.LazyLoading;
using HR_System.Core.Interfaces.RepositoryContracts;
using HR_System.Core.Interfaces.ServiceContracts.IActivitiesService;

namespace HR_System.Infrastructure.Services;
using System.Net;

public class ActivitiesService(IActivityRepository activityRepository) : IActivitiesService
{
    public async Task<Result<ActivityDTO>> AddAsync(ActivityAddDTO toAdd, Guid triggeredById, CancellationToken cancellationToken = default)
    {
        var activity = new Activity
        {
            Type = toAdd.Type,
            TaskId = toAdd.TaskId,
            ApprovalId = toAdd.ApprovalId,
            TriggeredById = triggeredById,
        };

        activityRepository.Add(activity);

        if (!await activityRepository.SaveChangesAsync(cancellationToken))
            return Result<ActivityDTO>.Failure("Failed saving activity to DB");

        return Result<ActivityDTO>.Success(activity.ToDTO());
    }

    public async Task<Result<IReadOnlyList<ActivityDTO>>> LazyGetAllSortedAsync(LazyDTO lazyData, CancellationToken cancellationToken = default)
    {
        if (lazyData.Taken < 0)
            return Result<IReadOnlyList<ActivityDTO>>.Failure("Taken cannot be negative", HttpStatusCode.BadRequest);

        var activities = await activityRepository.LazyGetAllSortedAsync(lazyData, cancellationToken);

        var result = activities.Select(a => a.ToDTO()).ToList();

        return Result<IReadOnlyList<ActivityDTO>>.Success(result);
    }
}
