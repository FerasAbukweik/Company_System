using System.Linq.Expressions;
using HR_System.Core.Domain.Entities;

namespace HR_System.Core.Abstraction.RepositoryContracts;

public interface INotificationsRepository
{
    /// <summary>
    /// Adds a new notification. Nothing is written to the DB until <see cref="SaveChangesAsync"/> is called.
    /// </summary>
    /// <param name="notification">The notification to add.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests (not used, adding is synchronous).</param>
    void Add(Notification notification, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the notifications that match the predicate, without change tracking.
    /// </summary>
    /// <param name="predicate">The condition every returned notification has to satisfy.</param>
    /// <param name="include">Navigation properties to load together with each notification. Null loads none.</param>
    /// <param name="orderBy">The property to sort by. Null keeps the database order.</param>
    /// <param name="orderByDesc">True to sort descending, false to sort ascending. Only used when <paramref name="orderBy"/> is set.</param>
    /// <param name="skip">How many matching notifications to skip (for paging). Null skips none.</param>
    /// <param name="take">The maximum number of notifications to return (for paging). Null returns all of them.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>The matching notifications, or an empty list when nothing matches.</returns>
    Task<IReadOnlyList<Notification>> FilterAsync(
        Expression<Func<Notification, bool>> predicate,
        Expression<Func<Notification, object?>>[]? include = null,
        Expression<Func<Notification, object?>>? orderBy = null,
        bool orderByDesc = false,
        int? skip = null,
        int? take = null,
        CancellationToken cancellationToken = default);

    /// <summary>Counts unread notifications belonging to the user.</summary>
    Task<int> GetUnreadCountAsync(Guid userId, CancellationToken cancellationToken = default);

    void SetRead(Notification notification);

    /// <summary>Writes all pending changes to the database.</summary>
    Task<bool> SaveChangesAsync(CancellationToken cancellationToken = default);
}
