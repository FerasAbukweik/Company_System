namespace HR_System.Core.Interfaces.ServiceContracts.IRedisService;

public interface IRedisGetService
{
    Task<T?> Get<T>(string key, CancellationToken cancellationToken = default);
}