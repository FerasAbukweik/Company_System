namespace HR_System.Core.Interfaces.ServiceContracts;

public interface IRedisService
{
    Task<T?> Get<T>(string key, CancellationToken cancellationToken = default);
    Task Set<T>(string key, T value, CancellationToken cancellationToken = default);
    
}