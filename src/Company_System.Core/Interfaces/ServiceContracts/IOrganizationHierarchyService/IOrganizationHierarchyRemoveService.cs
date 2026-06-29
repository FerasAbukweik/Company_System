using HR_System.Core.common;
using HR_System.Core.DTO.OrganizationHierarchy;

namespace HR_System.Core.Interfaces.ServiceContracts.IOrganizationHierarchyService;

public interface IOrganizationHierarchyRemoveService
{
    Task<Result<OrganizationHierarchyDTO>> RemoveAsync(Guid toRemoveId, Guid currUserId, CancellationToken cancellationToken = default);
}