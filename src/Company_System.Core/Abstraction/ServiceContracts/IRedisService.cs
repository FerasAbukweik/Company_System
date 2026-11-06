namespace HR_System.Core.Abstraction.ServiceContracts;

public interface IRedisService
{
    /// <summary>
    /// Gets a cached value.
    /// </summary>
    /// <typeparam name="T">The type the cached value was stored as.</typeparam>
    /// <param name="key">The cache key.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>The cached value, or the default of <typeparamref name="T"/> when the key isn't cached.</returns>
    Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default);

    /// <summary>
    /// Caches the value. A null expiration (in minutes) falls back to the value in RedisOptions.
    /// </summary>
    /// <typeparam name="T">The type of the value to cache.</typeparam>
    /// <param name="key">The cache key.</param>
    /// <param name="value">The value to cache.</param>
    /// <param name="absoluteExpirationMinutes">Minutes until the entry expires no matter what. Null uses the configured default.</param>
    /// <param name="slidingExpirationMinutes">Minutes without access until the entry expires. Null uses the configured default.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>A task that completes when the value is cached.</returns>
    Task SetAsync<T>(
        string key,
        T value,
        int? absoluteExpirationMinutes = null,
        int? slidingExpirationMinutes = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Removes one cached entry.
    /// </summary>
    /// <param name="key">The cache key.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>A task that completes when the entry is removed.</returns>
    Task RemoveAsync(string key, CancellationToken cancellationToken = default);

    /// <summary>
    /// Removes every cached entry whose key starts with the prefix.
    /// </summary>
    /// <param name="prefix">The start of the cache keys to remove.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>A task that completes when the matching entries are removed.</returns>
    Task RemoveByPrefixAsync(string prefix, CancellationToken cancellationToken = default);
}
