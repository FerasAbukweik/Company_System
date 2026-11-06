using HR_System.Core.Abstraction.ServiceContracts;
using HR_System.Core.DTO.Activity;
using HR_System.Core.DTO.LazyLoading;
using HR_System.Core.Result;
using HR_System.Infrastructure.Services;
using Microsoft.Extensions.Logging;

namespace HR_System.Infrastructure.Decorators.Loggers;

public class LoggingActivitiesService(
    IActivitiesService inner,
    ILogger<LoggingActivitiesService> logger) : IActivitiesService
{
    private const string ServiceName = nameof(ActivitiesService);

    public async Task<Result<ActivityDto>> AddAsync(ActivityAddDto toAdd, Guid triggeredById, CancellationToken cancellationToken = default)
    {
        var result = await inner.AddAsync(toAdd, triggeredById, cancellationToken);
        logger.LogResult(result, ServiceName, nameof(AddAsync), $"TriggeredById: {triggeredById}, Type: {toAdd.Type}");
        return result;
    }

    public async Task<Result<IReadOnlyList<ActivityDto>>> LazyGetAllSortedAsync(LazyDTO lazyData, Guid userId, CancellationToken cancellationToken = default)
    {
        var result = await inner.LazyGetAllSortedAsync(lazyData, userId, cancellationToken);
        logger.LogResult(result, ServiceName, nameof(LazyGetAllSortedAsync), $"UserId: {userId}", logSuccess: false);
        return result;
    }
}
