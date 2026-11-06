using HR_System.Core.Abstraction.ServiceContracts;
using HR_System.Core.Domain.Entities;
using HR_System.Core.Result;
using HR_System.Infrastructure.Services;
using Microsoft.Extensions.Logging;

namespace HR_System.Infrastructure.Decorators.Loggers;

public class LoggingRefreshTokenService(
    IRefreshTokenService inner,
    ILogger<LoggingRefreshTokenService> logger) : IRefreshTokenService
{
    private const string ServiceName = nameof(RefreshTokenService);

    public async Task<Result<string>> GenerateRefreshTokenAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var result = await inner.GenerateRefreshTokenAsync(userId, cancellationToken);
        // the token itself is deliberately not logged
        logger.LogResult(result, ServiceName, nameof(GenerateRefreshTokenAsync), $"UserId: {userId}");
        return result;
    }

    public async Task<Result<RefreshToken>> ConsumeRefreshTokenAsync(string tokenString, CancellationToken cancellationToken = default)
    {
        var result = await inner.ConsumeRefreshTokenAsync(tokenString, cancellationToken);
        logger.LogResult(result, ServiceName, nameof(ConsumeRefreshTokenAsync), $"UserId: {result.Value?.UserId}");
        return result;
    }
}
