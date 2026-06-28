using HR_System.Core.common;
using HR_System.Core.Domain.Identity;
using HR_System.Core.DTO.Token;

namespace HR_System.Core.Interfaces.ServiceContracts.ITokenServices;

public interface IGenerateTokenService
{
    Task<Result<string>> GenerateAccessTokenAsync(ApplicationUser user);
    Task<Result<string>> GenerateRefreshTokenAsync(Guid userId , CancellationToken cancellationToken = default);
    Task<Result<AccessAndRefreshTokenDTO>> GenerateNewAccessAndRefreshTokenAsync(ApplicationUser user, CancellationToken cancellationToken = default);
}