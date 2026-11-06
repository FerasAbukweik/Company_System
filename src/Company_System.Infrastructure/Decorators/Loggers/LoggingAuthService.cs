using HR_System.Core.Abstraction.ServiceContracts;
using HR_System.Core.DTO.Auth;
using HR_System.Core.Result;
using HR_System.Infrastructure.Services;
using Microsoft.Extensions.Logging;

namespace HR_System.Infrastructure.Decorators.Loggers;

public class LoggingAuthService(
    IAuthService inner,
    ILogger<LoggingAuthService> logger) : IAuthService
{
    private const string ServiceName = nameof(AuthService);

    public async Task<Result<UserDTO>> LoginAsync(LoginDTO loginData, CancellationToken cancellationToken = default)
    {
        var result = await inner.LoginAsync(loginData, cancellationToken);
        // credentials (email / password) are deliberately not logged
        logger.LogResult(result, ServiceName, nameof(LoginAsync), $"UserId: {result.Value?.UserId}");
        return result;
    }

    public Result Logout()
    {
        var result = inner.Logout();
        logger.LogResult(result, ServiceName, nameof(Logout));
        return result;
    }

    public async Task<Result> RefreshTokensAsync(CancellationToken cancellationToken = default)
    {
        var result = await inner.RefreshTokensAsync(cancellationToken);
        logger.LogResult(result, ServiceName, nameof(RefreshTokensAsync));
        return result;
    }
}
