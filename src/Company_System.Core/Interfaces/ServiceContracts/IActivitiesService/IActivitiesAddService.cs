using HR_System.Core.common;
using HR_System.Core.DTO.Activity;

namespace HR_System.Core.Interfaces.ServiceContracts.IActivitiesService;

public interface IActivitiesAddService
{
    Task<Result<ActivityDTO>> AddAsync(ActivityAddDTO toAdd, Guid triggeredById, CancellationToken cancellationToken = default);
}