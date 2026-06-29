using HR_System.Core.common;
using HR_System.Core.DTO.Activity;
using HR_System.Core.DTO.LazyLoading;

namespace HR_System.Core.Interfaces.ServiceContracts;

public interface IActivitiesService
{
    Task<Result<ActivityDTO>> AddAsync(ActivityAddDTO toAdd, Guid triggeredById, CancellationToken cancellationToken = default);
    Task<Result<IReadOnlyList<ActivityDTO>>> LazyGetAllSortedAsync(LazyDTO lazyData, CancellationToken cancellationToken = default);
    
}