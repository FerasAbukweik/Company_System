using HR_System.Core.Domain.Entities;
using HR_System.Core.DTO.OrganizationHierarchy;

namespace HR_System.Core.Interfaces.RepositoryContracts;

public interface IOrganizationHierarchyRepository
{
    void Add(OrganizationHierarchy toAdd);
    Task<IReadOnlyList<OrganizationHierarchy>> GetChildrenAsync(IReadOnlyList<Guid>? parents, CancellationToken cancellationToken = default);
    Task<OrganizationHierarchy?> RemoveAsync(Guid toRemoveId, CancellationToken cancellationToken = default);
    Task<bool> SaveChangesAsync(CancellationToken cancellationToken = default);
}