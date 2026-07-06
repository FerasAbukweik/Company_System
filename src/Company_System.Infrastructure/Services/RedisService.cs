using System.Text.Json;
using HR_System.Core.Interfaces.ServiceContracts;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Configuration;

namespace HR_System.Infrastructure.Services;

public class RedisService(IDistributedCache cache,
    IConfiguration configuration) : IRedisService
{
    
    public async Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default)
    {
        var data = await cache.GetAsync(key, cancellationToken);
        
        if (data == null)
            return default(T);

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
}