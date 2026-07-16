using HR_System.Core.common;
using HR_System.Core.Domain.Entities;
using HR_System.Core.DTO.Activity;
using HR_System.Core.DTO.LazyLoading;
using HR_System.Core.Interfaces.RepositoryContracts;
using HR_System.Core.Interfaces.ServiceContracts;
using Microsoft.Extensions.Logging;

namespace HR_System.Infrastructure.Services;

public class ActivitiesService(
    IActivityRepository activityRepository,
    ILogger<ActivitiesService> logger
    ) : IActivitiesService
{
    public async Task<Result<ActivityDTO>> AddAsync(ActivityAddDTO toAdd, Guid triggeredById, CancellationToken cancellationToken = default)
    {
        var activity = new Activity
        {
            Type = toAdd.Type,
            TriggeredById = triggeredById,
            Title = toAdd.Title,
            Description = toAdd.Description 
        };

        activityRepository.Add(activity);

        if (!await activityRepository.SaveChangesAsync(cancellationToken))
        {
            logger.LogError("{serviceName}.{methodName} failed saving changes to DB",
                nameof(AccountService), nameof(AddAsync));
            return Result<ActivityDTO>.Failure("Failed saving changes to DB");
        }
        
        logger.LogInformation("{serviceName}.{methodName} activity was added by userId: {userId}",
            nameof(AccountService), nameof(AddAsync), triggeredById);
        
        return Result<ActivityDTO>.Success(activity.ToDTO());
    }

    public async Task<Result<IReadOnlyList<ActivityDTO>>> LazyGetAllSortedAsync(LazyDTO lazyData, Guid userId, CancellationToken cancellationToken = default)
    {
        var activities = await activityRepository.LazyGetAllSortedAsync(lazyData, userId, cancellationToken);

        var result = activities.Select(a => a.ToDTO()).ToList();

        return Result<IReadOnlyList<ActivityDTO>>.Success(result);
    }
}
