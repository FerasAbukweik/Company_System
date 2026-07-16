using HR_System.Core.common;
using HR_System.Core.Domain.Identity;
using HR_System.Core.DTO.Token;

namespace HR_System.Core.Interfaces.ServiceContracts;

public interface ITokensService
{
    /// <summary>
    /// Generates both a fresh access token and a refresh token for a given user.
    /// </summary>
    Task<Result<AccessAndRefreshTokenDTO>> GenerateNewTokensAsync(ApplicationUser user, CancellationToken cancellationToken = default);

    /// <summary>
    /// Validates the existing refresh token extracted from HTTP cookies, revokes it, 
    /// and issues a new pair of tokens.
    /// </summary>
    Task<Result<AccessAndRefreshTokenDTO>> RegenerateTokensAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Orchestrates the entire token rotation sequence and automatically updates the client-side cookies with the new values.
    /// </summary>
    Task<Result<AccessAndRefreshTokenDTO>> UpdateUserTokensAsync(CancellationToken cancellationToken = default);
}