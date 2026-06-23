using HR_System.Core.Domain.Idnetity;
using HR_System.Core.DTO;
using HR_System.Core.Helpers;

namespace HR_System.Core.ServiceContracts.ITokenServices;

public interface IGenerateTokenService
{
    Task<Result<string>> GenerateAccessTokenAsync(ApplicationUser user);
    Task<Result<string>> GenerateRefreshTokenAsync(Guid userId , CancellationToken cancellationToken = default);
    Task<Result<AccessAndRefreshTokenDTO>> GenerateNewAccessAndRefreshToken(ApplicationUser? user = null);
}