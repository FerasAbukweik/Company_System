namespace HR_System.Core.Interfaces.ServiceContracts.IRedisService;

public interface IRedisSetService
{
    Task Set<T>(string key, T value, CancellationToken cancellationToken = default);
}