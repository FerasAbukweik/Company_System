using HR_System.Core.Abstraction.ServiceContracts;
using HR_System.Core.DTO.LazyLoading;
using HR_System.Core.DTO.OrganizationHierarchy;
using HR_System.Core.Result;

namespace HR_System.Infrastructure.Decorators.Caching;

public class CachedOrganizationHierarchyService : IOrganizationHierarchyService
{
    private readonly IOrganizationHierarchyService _innerService;
    private readonly IRedisService _redisService;

    private const string CachePrefix = "hierarchy";

    public CachedOrganizationHierarchyService(
        IOrganizationHierarchyService innerService,
        IRedisService redisService)
    {
        _innerService = innerService;
        _redisService = redisService;
    }


    public async Task<Result<OrganizationHierarchyDTO>> AddAsync(OrganizationHierarchyAddDTO toAdd, CancellationToken cancellationToken = default)
    {
        var result = await _innerService.AddAsync(toAdd, cancellationToken);
        if (result.IsSuccess)
        {
            await ClearHierarchyCacheAsync();
        }
        return result;
    }

    public async Task<Result<OrganizationHierarchyDTO>> RemoveAsync(Guid toRemoveId, Guid currUserId, CancellationToken cancellationToken = default)
    {
        var result = await _innerService.RemoveAsync(toRemoveId, currUserId, cancellationToken);
        if (result.IsSuccess)
        {
            await ClearHierarchyCacheAsync();
        }
        return result;
    }
    
    // get all children for parents
    public async Task<Result<IReadOnlyDictionary<Guid, IReadOnlyList<OrganizationHierarchyDTO>>>> GetChildrenAsync(
        IReadOnlyList<Guid>? parents, 
        CancellationToken cancellationToken = default)
    {
        // Generate unique cache key based on parent list
        string parentsKey = parents != null && parents.Any() 
            ? string.Join("-", parents.OrderBy(p => p)) 
            : "root";
        
        // Version the node response so cached records without FullName are not reused.
        string cacheKey = $"{CachePrefix}:children:v2:parents:{parentsKey}";

        // Try Redis
        var cachedData = await _redisService.GetAsync<Dictionary<Guid, List<OrganizationHierarchyDTO>>>(cacheKey, cancellationToken);
        if (cachedData != null)
        {
            var readOnlyResult = cachedData.ToDictionary(
                kvp => kvp.Key,
                kvp => (IReadOnlyList<OrganizationHierarchyDTO>)kvp.Value.AsReadOnly()
            );
            return Result<IReadOnlyDictionary<Guid, IReadOnlyList<OrganizationHierarchyDTO>>>.Success(readOnlyResult);
        }

        // Cache Miss -> DB
        var dbResult = await _innerService.GetChildrenAsync(parents, cancellationToken);
        if (!dbResult.IsSuccess) return dbResult;

        // Save DTO to Redis
        var serializableDict = dbResult.Value!.ToDictionary(
            kvp => kvp.Key,
            kvp => kvp.Value.ToList()
        );
        await _redisService.SetAsync(cacheKey, serializableDict, cancellationToken: cancellationToken);

        return dbResult;
    }

    // get all user parents ids 
    public async Task<Result<IReadOnlyList<Guid>>> GetParentUserIds(Guid userId, CancellationToken cancellationToken = default)
    {
        string cacheKey = $"{CachePrefix}:parentids:user:{userId}";

        var cachedData = await _redisService.GetAsync<List<Guid>>(cacheKey, cancellationToken);
        if (cachedData != null)
        {
            return Result<IReadOnlyList<Guid>>.Success(cachedData.AsReadOnly());
        }

        var dbResult = await _innerService.GetParentUserIds(userId, cancellationToken);
        if (!dbResult.IsSuccess) return dbResult;

        await _redisService.SetAsync(cacheKey, dbResult.Value!.ToList(), cancellationToken: cancellationToken);

        return dbResult;
    }

    // get usernames for all users with their hierarchy ids
    public async Task<Result<IReadOnlyList<UserNameDTO>>> GetUserNames(LazyDTO lazyData, CancellationToken cancellationToken = default)
    {
        string lazyKey = $"{lazyData.Taken}_{lazyData.SectionSize}";
        string cacheKey = $"{CachePrefix}:usernames:lazy:{lazyKey}";

        var cachedData = await _redisService.GetAsync<List<UserNameDTO>>(cacheKey, cancellationToken);
        if (cachedData != null)
        {
            return Result<IReadOnlyList<UserNameDTO>>.Success(cachedData.AsReadOnly());
        }

        var dbResult = await _innerService.GetUserNames(lazyData, cancellationToken);
        if (!dbResult.IsSuccess) return dbResult;

        await _redisService.SetAsync(cacheKey, dbResult.Value!.ToList(), cancellationToken: cancellationToken);

        return dbResult;
    }

    // clears all hierarchy cache
    private async Task ClearHierarchyCacheAsync()
    {
        try
        {
            // Deletes every key starting with "hierarchy:" in one sweep
            await _redisService.RemoveByPrefixAsync($"{CachePrefix}"); 
        }
        catch (Exception)
        {
            // a cache failure must not fail a write that already reached the DB;
            // LoggingRedisService has already logged the failure
        }
    }
}