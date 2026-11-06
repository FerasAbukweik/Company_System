using HR_System.Core.Abstraction.ServiceContracts;
using HR_System.Infrastructure.Services;
using Microsoft.Extensions.Logging;

namespace HR_System.Infrastructure.Decorators.Loggers;

/// <summary>
/// Logging decorator for <see cref="IRedisService"/>. Only failures are logged (cache hits and
/// misses are far too frequent to be useful); the exception is rethrown so callers decide
/// whether a cache failure is fatal.
/// </summary>
public class LoggingRedisService(
    IRedisService inner,
    ILogger<LoggingRedisService> logger) : IRedisService
{
    private const string ServiceName = nameof(RedisService);

    public async Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default)
    {
        try { return await inner.GetAsync<T>(key, cancellationToken); }
        catch (Exception ex) when (LogFailure(ex, nameof(GetAsync), key)) { throw; }
    }

    public async Task SetAsync<T>(
        string key,
        T value,
        int? absoluteExpirationMinutes = null,
        int? slidingExpirationMinutes = null,
        CancellationToken cancellationToken = default)
    {
        try { await inner.SetAsync(key, value, absoluteExpirationMinutes, slidingExpirationMinutes, cancellationToken); }
        catch (Exception ex) when (LogFailure(ex, nameof(SetAsync), key)) { throw; }
    }

    public async Task RemoveAsync(string key, CancellationToken cancellationToken = default)
    {
        try { await inner.RemoveAsync(key, cancellationToken); }
        catch (Exception ex) when (LogFailure(ex, nameof(RemoveAsync), key)) { throw; }
    }

    public async Task RemoveByPrefixAsync(string prefix, CancellationToken cancellationToken = default)
    {
        try { await inner.RemoveByPrefixAsync(prefix, cancellationToken); }
        catch (Exception ex) when (LogFailure(ex, nameof(RemoveByPrefixAsync), prefix)) { throw; }
    }

    // exception filter: logs, then returns false so the exception keeps propagating untouched
    private bool LogFailure(Exception ex, string method, string key)
    {
        logger.LogError(ex, "{ServiceName}.{MethodName} failed for key {Key}", ServiceName, method, key);
        return false;
    }
}
