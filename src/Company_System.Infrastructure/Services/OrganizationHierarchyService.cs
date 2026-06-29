using System.Collections.Immutable;
using System.Net;
using HR_System.Core.common;
using HR_System.Core.Domain.Entities;
using HR_System.Core.DTO.OrganizationHierarchy;
using HR_System.Core.Interfaces.RepositoryContracts;
using HR_System.Core.Interfaces.ServiceContracts.IOrganizationHierarchyService;

namespace HR_System.Infrastructure.Services;

public class OrganizationHierarchyService(IOrganizationHierarchyRepository hierarchyRepository) : IOrganizationHierarchyService
{
    public async Task<Result<OrganizationHierarchyDTO>> AddAsync(OrganizationHierarchyAddDTO toAdd, Guid currUserId, CancellationToken cancellationToken = default)
    {
        var toAdd_DB = new OrganizationHierarchy()
        {
            Position = toAdd.Position,
            UserId = currUserId,
            ParentId = toAdd.ParentId,
        };
        hierarchyRepository.Add(toAdd_DB);

        if(!(await hierarchyRepository.SaveChangesAsync(cancellationToken)))
            return Result<OrganizationHierarchyDTO>.Failure("Failed to add organization hierarchy");

        return Result<OrganizationHierarchyDTO>.Success(toAdd_DB.ToDTO(currUserId));
    }

    public async Task<Result<IReadOnlyList<OrganizationHierarchyDTO>>> GetChildrenAsync(Guid currUserId, IReadOnlyList<Guid>? parents, CancellationToken cancellationToken = default)
    {
        var children = (await hierarchyRepository.GetChildrenAsync(parents, cancellationToken))
            .Select(h => h.ToDTO(currUserId));

        return Result<IReadOnlyList<OrganizationHierarchyDTO>>.Success(children.ToImmutableList());
    }

    public async Task<Result<OrganizationHierarchyDTO>> RemoveAsync(Guid toRemoveId, Guid currUserId, CancellationToken cancellationToken = default)
    {
        // TODO check curr user have access
        var removed = await hierarchyRepository.RemoveAsync(toRemoveId, cancellationToken);
        if(removed == null)
            return Result<OrganizationHierarchyDTO>.Failure("hierarchy not found", HttpStatusCode.BadRequest);

        if(removed.ParentId == null)
            return Result<OrganizationHierarchyDTO>.Failure("cannt remove root employee", HttpStatusCode.BadRequest);
            
        if(!(await hierarchyRepository.SaveChangesAsync(cancellationToken)))
            return Result<OrganizationHierarchyDTO>.Failure("failed saving changes to DB");
        
        
        return Result<OrganizationHierarchyDTO>.Success(removed.ToDTO(currUserId));
    }
}