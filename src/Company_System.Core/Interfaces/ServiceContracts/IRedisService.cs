namespace HR_System.Core.Interfaces.ServiceContracts;

public interface IRedisService
{
    Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default);
    Task SetAsync<T>(string key, T value, CancellationToken cancellationToken = default);
    Task RemoveAsync(string key, CancellationToken cancellationToken = default); 
    Task RemoveByPrefixAsync(string prefix, CancellationToken cancellationToken = default);
}