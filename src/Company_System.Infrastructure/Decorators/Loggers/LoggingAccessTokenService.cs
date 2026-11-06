using HR_System.Core.Abstraction.ServiceContracts;
using HR_System.Core.Domain.Identity;
using HR_System.Core.DTO.Token;
using HR_System.Core.Result;
using HR_System.Infrastructure.Services;
using Microsoft.Extensions.Logging;

namespace HR_System.Infrastructure.Decorators.Loggers;

public class LoggingAccessTokenService(
    IAccessTokenService inner,
    ILogger<LoggingAccessTokenService> logger) : IAccessTokenService
{
    private const string ServiceName = nameof(AccessTokenService);

    public async Task<Result<AccessTokenResult>> GenerateAccessTokenAsync(ApplicationUser user)
    {
        var result = await inner.GenerateAccessTokenAsync(user);
        // the token itself is deliberately not logged
        logger.LogResult(result, ServiceName, nameof(GenerateAccessTokenAsync), $"UserId: {user.Id}");
        return result;
    }
}
