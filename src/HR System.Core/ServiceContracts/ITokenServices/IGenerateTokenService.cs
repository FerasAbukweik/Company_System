using HR_System.Core.Domain.Idnetity;
using HR_System.Core.Helpers;

namespace HR_System.Core.ServiceContracts;

public interface IGenerateTokenService
{
    Task<Result<string>> GenerateAccessTokenAsync(ApplicationUser user);
    Task<Result<string>> GenerateRefreshTokenAsync(Guid userId , CancellationToken cancellationToken = default);
}