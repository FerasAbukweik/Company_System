using HR_System.Core.common;
using HR_System.Core.DTO.Activity;
using HR_System.Core.DTO.LazyLoading;

namespace HR_System.Core.Interfaces.ServiceContracts.IActivitiesService;

public interface IActivitiesGetService
{
    Task<Result<IReadOnlyList<ActivityDTO>>> LazyGetAllSortedAsync(LazyDTO lazyData, CancellationToken cancellationToken = default);
}