using HR_System.Core.common;
using HR_System.Core.Domain.Entities;

namespace HR_System.Core.Interfaces.ServiceContracts;

public interface IRefreshTokenService
{
    /// <summary>
    /// Generates a cryptographically strong refresh token and registers it in the persistent database.
    /// </summary>
    /// <param name="userId">The unique identifier of the user.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>A result containing the raw refresh token string if successful.</returns>
    Task<Result<string>> GenerateRefreshTokenAsync(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Safely consumes a refresh token by finding it, removing it from storage to prevent replay attacks, 
    /// and validating its expiration.
    /// </summary>
    /// <param name="tokenString">The raw token string to look up and consume.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>A result containing the removed RefreshToken entity if valid; otherwise, a failure.</returns>
    Task<Result<RefreshToken>> ConsumeRefreshTokenAsync(string tokenString, CancellationToken cancellationToken = default);
}