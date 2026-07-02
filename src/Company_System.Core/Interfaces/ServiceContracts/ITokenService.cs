using HR_System.Core.common;
using HR_System.Core.Domain.Identity;
using HR_System.Core.DTO.Token;

namespace HR_System.Core.Interfaces.ServiceContracts;

public interface ITokenService
{
    Task<Result<string>> GenerateAccessTokenAsync(ApplicationUser user);
    Task<Result<string>> GenerateRefreshTokenAsync(Guid userId , CancellationToken cancellationToken = default);
    Task<Result<AccessAndRefreshTokenDTO>> GenerateNewTokensAsync(ApplicationUser user, CancellationToken cancellationToken = default);
    Task<Result<AccessAndRefreshTokenDTO>> RegenerateTokensAsync(CancellationToken cancellationToken = default);
    Task<Result<AccessAndRefreshTokenDTO>> UpdateUserTokensAsync(CancellationToken cancellationToken = default);
}