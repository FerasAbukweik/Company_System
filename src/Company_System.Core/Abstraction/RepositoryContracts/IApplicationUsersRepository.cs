using System.Linq.Expressions;
using HR_System.Core.Domain.Identity;
using HR_System.Core.DTO.Users;
using HR_System.Core.Domain.Entities;

namespace HR_System.Core.Abstraction.RepositoryContracts;

public interface IApplicationUsersRepository
{
    /// <summary>
    /// Gets a user by id, without change tracking.
    /// </summary>
    /// <param name="id">The id of the user.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>The user, or null when it doesn't exist.</returns>
    Task<ApplicationUser?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a user by id together with the given navigation properties, without change tracking.
    /// </summary>
    /// <param name="id">The id of the user.</param>
    /// <param name="include">Navigation properties to load together with the user.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>The user, or null when it doesn't exist.</returns>
    Task<ApplicationUser?> GetByIdAsync(
        Guid id,
        Expression<Func<ApplicationUser, object?>>[] include,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a user by email (matched against the normalized email), without change tracking.
    /// </summary>
    /// <param name="email">The email of the user, any casing.</param>
    /// <param name="include">Navigation properties to load together with the user. Null loads none.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>The user, or null when no user has this email.</returns>
    Task<ApplicationUser?> GetByEmailAsync(
        string email,
        Expression<Func<ApplicationUser, object?>>[]? include = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the users that match the predicate, without change tracking.
    /// </summary>
    /// <param name="predicate">The condition every returned user has to satisfy.</param>
    /// <param name="include">Navigation properties to load together with each user. Null loads none.</param>
    /// <param name="orderBy">The property to sort by. Null keeps the database order.</param>
    /// <param name="orderByDesc">True to sort descending, false to sort ascending. Only used when <paramref name="orderBy"/> is set.</param>
    /// <param name="skip">How many matching users to skip (for paging). Null skips none.</param>
    /// <param name="take">The maximum number of users to return (for paging). Null returns all of them.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>The matching users, or an empty list when nothing matches.</returns>
    Task<IReadOnlyList<ApplicationUser>> FilterAsync(
        Expression<Func<ApplicationUser, bool>> predicate,
        Expression<Func<ApplicationUser, object?>>[]? include = null,
        Expression<Func<ApplicationUser, object?>>? orderBy = null,
        bool orderByDesc = false,
        int? skip = null,
        int? take = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Writes all pending changes to the DB.
    /// </summary>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>True when at least one row was written, otherwise false.</returns>
    Task<int> CountAsync(Expression<Func<ApplicationUser, bool>> predicate, CancellationToken cancellationToken = default);

    Task<EmployeeSummaryDTO> GetSummaryAsync(CancellationToken cancellationToken = default);

    Task ReplaceImageAsync(Guid userId, Image image, CancellationToken cancellationToken = default);

    Task<bool> SaveChangesAsync(CancellationToken cancellationToken = default);
}
