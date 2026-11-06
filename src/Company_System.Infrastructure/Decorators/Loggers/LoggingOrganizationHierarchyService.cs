using HR_System.Core.Abstraction.ServiceContracts;
using HR_System.Core.DTO.LazyLoading;
using HR_System.Core.DTO.OrganizationHierarchy;
using HR_System.Core.Result;
using HR_System.Infrastructure.Services;
using Microsoft.Extensions.Logging;

namespace HR_System.Infrastructure.Decorators.Loggers;

public class LoggingOrganizationHierarchyService(
    IOrganizationHierarchyService inner,
    ILogger<LoggingOrganizationHierarchyService> logger) : IOrganizationHierarchyService
{
    private const string ServiceName = nameof(OrganizationHierarchyService);

    public async Task<Result<OrganizationHierarchyDTO>> AddAsync(OrganizationHierarchyAddDTO toAdd, CancellationToken cancellationToken = default)
    {
        var result = await inner.AddAsync(toAdd, cancellationToken);
        logger.LogResult(result, ServiceName, nameof(AddAsync),
            $"UserId: {toAdd.UserId}, ParentId: {toAdd.ParentId}, HierarchyId: {result.Value?.Id}");
        return result;
    }

    public async Task<Result<IReadOnlyDictionary<Guid, IReadOnlyList<OrganizationHierarchyDTO>>>> GetChildrenAsync(IReadOnlyList<Guid>? parents, CancellationToken cancellationToken = default)
    {
        var result = await inner.GetChildrenAsync(parents, cancellationToken);
        logger.LogResult(result, ServiceName, nameof(GetChildrenAsync), $"ParentsCount: {parents?.Count ?? 0}", logSuccess: false);
        return result;
    }

    public async Task<Result<OrganizationHierarchyDTO>> RemoveAsync(Guid toRemoveId, Guid currUserId, CancellationToken cancellationToken = default)
    {
        var result = await inner.RemoveAsync(toRemoveId, currUserId, cancellationToken);
        logger.LogResult(result, ServiceName, nameof(RemoveAsync), $"HierarchyId: {toRemoveId}, CurrentUserId: {currUserId}");
        return result;
    }

    public async Task<Result<IReadOnlyList<Guid>>> GetParentUserIds(Guid userId, CancellationToken cancellationToken = default)
    {
        var result = await inner.GetParentUserIds(userId, cancellationToken);
        logger.LogResult(result, ServiceName, nameof(GetParentUserIds), $"UserId: {userId}", logSuccess: false);
        return result;
    }

    public async Task<Result<IReadOnlyList<UserNameDTO>>> GetUserNames(LazyDTO lazyData, CancellationToken cancellationToken = default)
    {
        var result = await inner.GetUserNames(lazyData, cancellationToken);
        logger.LogResult(result, ServiceName, nameof(GetUserNames), "", logSuccess: false);
        return result;
    }
}
