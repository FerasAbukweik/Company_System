using HR_System.Core.common;
using HR_System.Core.DTO.OrganizationHierarchy;

namespace HR_System.Core.Interfaces.ServiceContracts.IOrganizationHierarchyService;

public interface IOrganizationHierarchyAddService
{
    Task<Result<OrganizationHierarchyDTO>> AddAsync(OrganizationHierarchyAddDTO toAdd,Guid currUserId, CancellationToken cancellationToken = default);
}