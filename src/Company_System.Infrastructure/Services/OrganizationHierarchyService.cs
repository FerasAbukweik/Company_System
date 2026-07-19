using System.Collections.Immutable;
using System.Net;
using HR_System.Core.common;
using HR_System.Core.Domain.Entities;
using HR_System.Core.DTO.LazyLoading;
using HR_System.Core.DTO.OrganizationHierarchy;
using HR_System.Core.Interfaces.RepositoryContracts;
using HR_System.Core.Interfaces.ServiceContracts;
using Microsoft.Extensions.Logging;

namespace HR_System.Infrastructure.Services;

public class OrganizationHierarchyService(
    IOrganizationHierarchyRepository hierarchyRepository,
    ILogger<OrganizationHierarchyService> logger
    ) : IOrganizationHierarchyService
{
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
            logger.LogError("{serviceName}.{methodName} failed saving changes to DB",
                nameof(MessagesService), nameof(AddAsync));
            return Result<OrganizationHierarchyDTO>.Failure("Failed to add organization hierarchy");
        }
        
        logger.LogError("{serviceName}.{methodName} OrganizationHierarchy with id of {OrganizationHierarchId} was created",
            nameof(MessagesService), nameof(AddAsync), toAdd_DB.Id);
        
        return Result<OrganizationHierarchyDTO>.Success(toAdd_DB.ToDTO());
    }
    public async Task<Result<IReadOnlyDictionary<Guid, IReadOnlyList<OrganizationHierarchyDTO>>>> GetChildrenAsync(IReadOnlyList<Guid>? parents, CancellationToken cancellationToken = default)
    {
        var children = await hierarchyRepository.GetChildrenAsync(parents, cancellationToken);
        
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
                if(child.ParentId != null)
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
        var removed = await hierarchyRepository.RemoveAsync(toRemoveId, cancellationToken);
        if (removed == null)
        {
            logger.LogWarning("{serviceName}.{methodName} user with id {currUserId} tried removing Hierarchy with id {HierarchyId} which doesnt exist",
                nameof(MessagesService), nameof(RemoveAsync), currUserId, toRemoveId);
            return Result<OrganizationHierarchyDTO>.Failure("hierarchy not found", HttpStatusCode.BadRequest);
        }

        if (removed.ParentId == null)
        {
            logger.LogWarning("{serviceName}.{methodName} someone withID: {userId} tried deleting user with Id {otherUserId} who is a possible CEO/admin/tree root",
                nameof(MessagesService), nameof(RemoveAsync), currUserId, removed.UserId);
            return Result<OrganizationHierarchyDTO>.Failure("cannt remove root employee", HttpStatusCode.BadRequest);
        }

        if (!(await hierarchyRepository.SaveChangesAsync(cancellationToken)))
        {
            logger.LogError("{serviceName}.{methodName} failed saving changes to DB",
                nameof(MessagesService), nameof(RemoveAsync));
            return Result<OrganizationHierarchyDTO>.Failure("failed saving changes to DB");
        }
        
        logger.LogError("{serviceName}.{methodName} OrganizationHierarchy with id of {OrganizationHierarchyId} was removed",
            nameof(MessagesService), nameof(RemoveAsync), removed.Id);
        
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