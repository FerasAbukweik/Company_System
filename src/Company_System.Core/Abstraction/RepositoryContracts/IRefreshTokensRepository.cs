using System.Linq.Expressions;
using HR_System.Core.Domain.Entities;

namespace HR_System.Core.Abstraction.RepositoryContracts;

public interface IRefreshTokensRepository
{
    /// <summary>
    /// Gets a refresh token by its id, without change tracking.
    /// </summary>
    /// <param name="id">The id of the refresh token.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>The refresh token, or null when it doesn't exist.</returns>
    Task<RefreshToken?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a refresh token by its id together with the given navigation properties, without change tracking.
    /// </summary>
    /// <param name="id">The id of the refresh token.</param>
    /// <param name="include">Navigation properties to load together with the refresh token.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>The refresh token, or null when it doesn't exist.</returns>
    Task<RefreshToken?> GetByIdAsync(
        Guid id,
        Expression<Func<RefreshToken, object?>>[] include,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Adds a new refresh token. Nothing is written to the DB until <see cref="SaveChangesAsync"/> is called.
    /// </summary>
    /// <param name="refreshToken">The refresh token to add.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests (not used, adding is synchronous).</param>
    void AddAsync(RefreshToken refreshToken, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes every expired refresh token straight in the DB.
    /// </summary>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>The tokens that were expired (and are now deleted), or an empty list when there were none.</returns>
    Task<IReadOnlyList<RefreshToken>> RemoveExpiredRefreshTokensAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a refresh token by its token string, without change tracking.
    /// </summary>
    /// <param name="refreshTokenString">The raw token string.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>The refresh token, or null when no token has this string.</returns>
    Task<RefreshToken?> GetByTokenAsync(string refreshTokenString, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes the token straight in the DB. A false result means it was already gone,
    /// which is how a concurrent second use of a single-use token is detected.
    /// </summary>
    /// <param name="refreshToken">The refresh token to delete.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>True when this call deleted the token, false when it no longer existed.</returns>
    Task<bool> RemoveAsync(RefreshToken refreshToken, CancellationToken cancellationToken = default);

    /// <summary>
    /// Writes all pending changes to the DB.
    /// </summary>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>True when at least one row was written, otherwise false.</returns>
    Task<bool> SaveChangesAsync(CancellationToken cancellationToken = default);
}
