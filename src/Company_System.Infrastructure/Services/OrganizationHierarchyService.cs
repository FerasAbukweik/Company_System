using System.Linq.Expressions;
using System.Net;
using HR_System.Core.Abstraction.RepositoryContracts;
using HR_System.Core.Abstraction.ServiceContracts;
using HR_System.Core.Domain.Entities;
using HR_System.Core.DTO.LazyLoading;
using HR_System.Core.DTO.OrganizationHierarchy;
using HR_System.Core.Result;

namespace HR_System.Infrastructure.Services;

public class OrganizationHierarchyService(
    IOrganizationHierarchyRepository hierarchyRepository
    ) : IOrganizationHierarchyService
{
    // how many layers of the tree one GetChildrenAsync call loads (the default section size)
    private const int MaxLayers = 5;

    public async Task<Result<OrganizationHierarchyDTO>> AddAsync(OrganizationHierarchyAddDTO toAdd, CancellationToken cancellationToken = default)
    {
        var toAdd_DB = new OrganizationHierarchy()
        {
            UserId = toAdd.UserId,
            ParentId = toAdd.ParentId,
        };
        hierarchyRepository.Add(toAdd_DB);

        if (!(await hierarchyRepository.SaveChangesAsync(cancellationToken)))
        {
            return Result<OrganizationHierarchyDTO>.Failure("Failed to add organization hierarchy");
        }


        return Result<OrganizationHierarchyDTO>.Success(toAdd_DB.ToDTO());
    }
    public async Task<Result<IReadOnlyDictionary<Guid, IReadOnlyList<OrganizationHierarchyDTO>>>> GetChildrenAsync(IReadOnlyList<Guid>? parents, CancellationToken cancellationToken = default)
    {
        // layer 1: the roots when no parents are supplied, otherwise the children of those parents
        Expression<Func<OrganizationHierarchy, bool>> firstLayerPredicate;
        if (parents == null || parents.Count == 0)
            firstLayerPredicate = o => o.IsActive && o.ParentId == null;
        else
            firstLayerPredicate = o => o.IsActive && o.ParentId != null && parents.Contains(o.ParentId.Value);

        // the user (with image) is needed on every layer
        Expression<Func<OrganizationHierarchy, object?>>[] include = [o => o.User!.Image];

        var children = await hierarchyRepository.FilterAsync(
            firstLayerPredicate,
            include,
            cancellationToken: cancellationToken);
        
        // layers 2..MaxLayers: one query per layer, each node gets the children found for it
        var currentLayer = children;
        for (var layer = 2; layer <= MaxLayers && currentLayer.Count > 0; layer++)
        {
            var currentIds = currentLayer.Select(o => o.Id).ToList();

            var nextLayer = await hierarchyRepository.FilterAsync(
                o => o.IsActive && o.ParentId != null && currentIds.Contains(o.ParentId.Value),
                include,
                cancellationToken: cancellationToken);

            var childrenByParent = nextLayer
                .GroupBy(o => o.ParentId!.Value)
                .ToDictionary(g => g.Key, g => g.ToList());

            foreach (var node in currentLayer)
            {
                if (childrenByParent.TryGetValue(node.Id, out var nodeChildren))
                    node.Children = nodeChildren;
            }

            currentLayer = nextLayer;
        }

        var result = new Dictionary<Guid, List<OrganizationHierarchyDTO>>();

        if (parents == null || !parents.Any())
        {
            result[Guid.Empty] = children.Select(c => c.ToDTO()).ToList();
        }
        else
        {
            foreach (var parent in parents)
            {
                result[parent] = new List<OrganizationHierarchyDTO>();
            }

            foreach (var child in children)
            {
                if (child.ParentId != null)
                    result[child.ParentId.Value].Add(child.ToDTO());
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
        // get the node with its branch marked inactive (not saved yet), then check it before saving
        var removed = await hierarchyRepository.DeactivateBranchAsync(toRemoveId, cancellationToken);
        if (removed == null)
        {
            return Result<OrganizationHierarchyDTO>.Failure("hierarchy not found", HttpStatusCode.BadRequest);
        }

        if (removed.ParentId == null)
        {
            return Result<OrganizationHierarchyDTO>.Failure("cannt remove root employee", HttpStatusCode.BadRequest);
        }

        // everything is ok, save
        if (!(await hierarchyRepository.SaveChangesAsync(cancellationToken)))
        {
            return Result<OrganizationHierarchyDTO>.Failure("failed saving changes to DB");
        }


        return Result<OrganizationHierarchyDTO>.Success(removed.ToDTO());
    }
    public async Task<Result<IReadOnlyList<Guid>>> GetParentUserIds(Guid userId, CancellationToken cancellationToken = default)
    {
        var result = await hierarchyRepository.GetParentUserIds(userId, cancellationToken);

        return Result<IReadOnlyList<Guid>>.Success(result);
    }
    public async Task<Result<IReadOnlyList<UserNameDTO>>> GetUserNames(LazyDTO lazyData, CancellationToken cancellationToken = default)
    {
        return Result<IReadOnlyList<UserNameDTO>>.Success(await hierarchyRepository.GetUserNames(lazyData, cancellationToken));
    }
}