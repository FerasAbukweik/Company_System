using HR_System.Core.common;
using HR_System.Core.DTO.OrganizationHierarchy;

namespace HR_System.Core.Interfaces.ServiceContracts.IOrganizationHierarchyService;

public interface IOrganizationHierarchyGetService
{
    Task<Result<IReadOnlyList<OrganizationHierarchyDTO>>> GetChildrenAsync(Guid currUserId, IReadOnlyList<Guid>? parents, CancellationToken cancellationToken = default);
}