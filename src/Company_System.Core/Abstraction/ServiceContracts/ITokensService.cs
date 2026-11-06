using HR_System.Core.Domain.Identity;
using HR_System.Core.DTO.Token;
using HR_System.Core.Result;

namespace HR_System.Core.Abstraction.ServiceContracts;

public interface ITokensService
{
    /// <summary>
    /// Generates both a fresh access token and a refresh token for a given user.
    /// </summary>
    /// <param name="user">The user the tokens are generated for.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>A result containing the new access and refresh tokens if successful.</returns>
    Task<Result<AccessAndRefreshTokenDTO>> GenerateNewTokensAsync(ApplicationUser user, CancellationToken cancellationToken = default);

    /// <summary>
    /// Validates the existing refresh token extracted from HTTP cookies, revokes it,
    /// and issues a new pair of tokens.
    /// </summary>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>A result containing the new access and refresh tokens, or a failure when the refresh token is missing, expired or already used.</returns>
    Task<Result<AccessAndRefreshTokenDTO>> RegenerateTokensAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Orchestrates the entire token rotation sequence and automatically updates the client-side cookies with the new values.
    /// </summary>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>A result containing the new tokens that were stored in the cookies.</returns>
    Task<Result<AccessAndRefreshTokenDTO>> UpdateUserTokensAsync(CancellationToken cancellationToken = default);
}
