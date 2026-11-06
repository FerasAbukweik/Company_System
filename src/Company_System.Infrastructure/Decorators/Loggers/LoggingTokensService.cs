using HR_System.Core.Abstraction.ServiceContracts;
using HR_System.Core.Domain.Identity;
using HR_System.Core.DTO.Token;
using HR_System.Core.Result;
using HR_System.Infrastructure.Services;
using Microsoft.Extensions.Logging;

namespace HR_System.Infrastructure.Decorators.Loggers;

public class LoggingTokensService(
    ITokensService inner,
    ILogger<LoggingTokensService> logger) : ITokensService
{
    private const string ServiceName = nameof(TokensService);

    public async Task<Result<AccessAndRefreshTokenDTO>> GenerateNewTokensAsync(ApplicationUser user, CancellationToken cancellationToken = default)
    {
        var result = await inner.GenerateNewTokensAsync(user, cancellationToken);
        logger.LogResult(result, ServiceName, nameof(GenerateNewTokensAsync), $"UserId: {user.Id}");
        return result;
    }

    public async Task<Result<AccessAndRefreshTokenDTO>> RegenerateTokensAsync(CancellationToken cancellationToken = default)
    {
        var result = await inner.RegenerateTokensAsync(cancellationToken);
        logger.LogResult(result, ServiceName, nameof(RegenerateTokensAsync));
        return result;
    }

    public async Task<Result<AccessAndRefreshTokenDTO>> UpdateUserTokensAsync(CancellationToken cancellationToken = default)
    {
        var result = await inner.UpdateUserTokensAsync(cancellationToken);
        logger.LogResult(result, ServiceName, nameof(UpdateUserTokensAsync));
        return result;
    }
}
