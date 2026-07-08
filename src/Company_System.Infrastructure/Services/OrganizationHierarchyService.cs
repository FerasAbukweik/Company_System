using System.Collections.Immutable;
using System.Net;
using HR_System.Core.common;
using HR_System.Core.Domain.Entities;
using HR_System.Core.DTO.OrganizationHierarchy;
using HR_System.Core.Interfaces.RepositoryContracts;
using HR_System.Core.Interfaces.ServiceContracts;

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

    public async Task<Result<IReadOnlyDictionary<Guid, IReadOnlyList<OrganizationHierarchyDTO>>>> GetChildrenAsync(Guid currUserId, IReadOnlyList<Guid>? parents, CancellationToken cancellationToken = default)
    {
        var children = await hierarchyRepository.GetChildrenAsync(parents, cancellationToken);
        
        var result = new Dictionary<Guid, List<OrganizationHierarchyDTO>>();

        if (parents == null || !parents.Any())
        {
            result[Guid.Empty] = children.Select(c => c.ToDTO(currUserId)).ToList();
        }
        else
        {
            foreach (var parent in parents)
            {
                result[parent] = new List<OrganizationHierarchyDTO>();
            }

            foreach (var child in children)
            {
                if(child.ParentId != null)
                    result[child.ParentId.Value].Add(child.ToDTO(currUserId));
            }
        }

        return Result<IReadOnlyDictionary<Guid, IReadOnlyList<OrganizationHierarchyDTO>>>.Success(
            result.ToDictionary(
                kvp => kvp.Key,
                kvp => (IReadOnlyList<OrganizationHierarchyDTO>)kvp.Value.AsReadOnly()
            )
        );
    }

    public async Task<Result<OrganizationHierarchyDTO>> RemoveAsync(Guid toRemoveId, Guid currUserId, CancellationToken cancellationToken = default)
    {
        var removed = await hierarchyRepository.RemoveAsync(toRemoveId, cancellationToken);
        if(removed == null)
            return Result<OrganizationHierarchyDTO>.Failure("hierarchy not found", HttpStatusCode.BadRequest);

        if(removed.ParentId == null)
            return Result<OrganizationHierarchyDTO>.Failure("cannt remove root employee", HttpStatusCode.BadRequest);
            
        if(!(await hierarchyRepository.SaveChangesAsync(cancellationToken)))
            return Result<OrganizationHierarchyDTO>.Failure("failed saving changes to DB");
        
        
        return Result<OrganizationHierarchyDTO>.Success(removed.ToDTO(currUserId));
    }

    public async Task<Result<IReadOnlyList<Guid>>> GetParentUserIds(Guid userId, CancellationToken cancellationToken = default)
    {
        var result = await hierarchyRepository.GetParentUserIds(userId, cancellationToken);

        return Result<IReadOnlyList<Guid>>.Success(result);
    }
}