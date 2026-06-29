using HR_System.Core.common;
using HR_System.Core.Domain.Entities;
using HR_System.Core.Domain.Identity;
using HR_System.Core.DTO.Token;

namespace HR_System.Core.Interfaces.ServiceContracts;

public interface ITokenService
{
    Task<Result<RefreshToken>> IsRefreshTokenValid(Guid userId, CancellationToken cancellationToken = default);
    Task<Result<string>> GenerateAccessTokenAsync(ApplicationUser user);
    Task<Result<string>> GenerateRefreshTokenAsync(Guid userId , CancellationToken cancellationToken = default);
    Task<Result<AccessAndRefreshTokenDTO>> GenerateNewAccessAndRefreshTokenAsync(ApplicationUser user, CancellationToken cancellationToken = default);
    
}