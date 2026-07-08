using HR_System.Core.common;
using HR_System.Core.DTO.OrganizationHierarchy;

namespace HR_System.Core.Interfaces.ServiceContracts;

public interface IOrganizationHierarchyService
{
    Task<Result<OrganizationHierarchyDTO>> AddAsync(OrganizationHierarchyAddDTO toAdd,Guid currUserId, CancellationToken cancellationToken = default);
    Task<Result<IReadOnlyDictionary<Guid, IReadOnlyList<OrganizationHierarchyDTO>>>> GetChildrenAsync(Guid currUserId, IReadOnlyList<Guid>? parents, CancellationToken cancellationToken = default);
    Task<Result<OrganizationHierarchyDTO>> RemoveAsync(Guid toRemoveId, Guid currUserId, CancellationToken cancellationToken = default);
    Task<Result<IReadOnlyList<Guid>>> GetParentUserIds(Guid userId, CancellationToken cancellationToken = default);
    
}