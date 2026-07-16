using HR_System.Core.Domain.Entities;
using HR_System.Core.DTO.LazyLoading;
using HR_System.Core.DTO.OrganizationHierarchy;

namespace HR_System.Core.Interfaces.RepositoryContracts;

public interface IOrganizationHierarchyRepository
{
    void Add(OrganizationHierarchy toAdd);
    Task<IReadOnlyList<OrganizationHierarchy>> GetChildrenAsync(IReadOnlyList<Guid> parents, CancellationToken cancellationToken = default);
    Task<OrganizationHierarchy?> RemoveAsync(Guid toRemoveId, CancellationToken cancellationToken = default);
    Task<bool> SaveChangesAsync(CancellationToken cancellationToken = default);
    Task<OrganizationHierarchy?> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);
    
    // <summary> Get users ids of all parents of supplied userId </summary>
    // <input> userId: userId for user we need his parents Ids </input>
    // <result> returns all userId for the parents of the supplied userId </result>
    Task<IReadOnlyList<Guid>> GetParentUserIds(Guid userId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<UserNameDTO>> GetUserNames(LazyDTO lazyData, CancellationToken cancellationToken = default);
}