using System.Text.Json;
using HR_System.Core.Interfaces.ServiceContracts;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Configuration;
using StackExchange.Redis;

namespace HR_System.Infrastructure.Services;

public class RedisService(
    IDistributedCache cache,
    IConnectionMultiplexer redisMultiplexer,
    IConfiguration configuration) : IRedisService
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
    
    public async Task SetAsync<T>(string key, T value, CancellationToken cancellationToken = default)
    {
        var options = new DistributedCacheEntryOptions()
        {
            AbsoluteExpiration = DateTimeOffset.Now.AddMinutes(configuration.GetValue<int>("Redis:AbsoluteExpiration")),
            SlidingExpiration = TimeSpan.FromMinutes(configuration.GetValue<int>("Redis:SlidingExpiration"))
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
        var instanceName = configuration.GetValue<string>("Redis:InstanceName") ?? string.Empty;
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