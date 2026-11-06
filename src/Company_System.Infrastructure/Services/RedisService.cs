using System.Text.Json;
using HR_System.Core.Abstraction.ServiceContracts;
using HR_System.Core.Options;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Options;
using StackExchange.Redis;

namespace HR_System.Infrastructure.Services;

public class RedisService(
    IDistributedCache cache,
    IConnectionMultiplexer redisMultiplexer,
    IOptions<RedisOptions> redisOptions) : IRedisService
{
    
    public async Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default)
    {
        var data = await cache.GetAsync(key, cancellationToken);
        
        if (data == null)
            return default;

        var options = new JsonSerializerOptions()
        {
            PropertyNameCaseInsensitive = true
        };
        
        return JsonSerializer.Deserialize<T>(data, options);
    }
    
    public async Task SetAsync<T>(
        string key,
        T value,
        int? absoluteExpirationMinutes = null,
        int? slidingExpirationMinutes = null,
        CancellationToken cancellationToken = default)
    {
        var options = new DistributedCacheEntryOptions()
        {
            AbsoluteExpiration = DateTimeOffset.Now.AddMinutes(absoluteExpirationMinutes ?? redisOptions.Value.AbsoluteExpiration),
            SlidingExpiration = TimeSpan.FromMinutes(slidingExpirationMinutes ?? redisOptions.Value.SlidingExpiration)
        };
        
        var serializedData = JsonSerializer.Serialize(value);
        
        await cache.SetStringAsync(key, serializedData,  options, cancellationToken);
    }

    public async Task RemoveAsync(string key, CancellationToken cancellationToken = default)
    {
        await cache.RemoveAsync(key, cancellationToken);
    }
    
    public async Task RemoveByPrefixAsync(string prefix, CancellationToken cancellationToken = default)
    {
        var instanceName = redisOptions.Value.InstanceName;
        var searchPattern = $"{instanceName}{prefix}*";

        var db = redisMultiplexer.GetDatabase();
        var endpoints = redisMultiplexer.GetEndPoints();

        foreach (var endpoint in endpoints)
        {
            var server = redisMultiplexer.GetServer(endpoint);

            await foreach (var key in server.KeysAsync(pattern: searchPattern).WithCancellation(cancellationToken))
            {
                await db.KeyDeleteAsync(key);
            }
        }
    }
}