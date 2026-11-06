using System.Linq.Expressions;
using HR_System.Core.Domain.Entities;

namespace HR_System.Core.Abstraction.RepositoryContracts;

public interface IActivityRepository
{
    /// <summary>
    /// Gets an activity by its id, without change tracking.
    /// </summary>
    /// <param name="id">The id of the activity.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>The activity, or null when it doesn't exist.</returns>
    Task<Activity?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets an activity by its id together with the given navigation properties, without change tracking.
    /// </summary>
    /// <param name="id">The id of the activity.</param>
    /// <param name="include">Navigation properties to load together with the activity.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>The activity, or null when it doesn't exist.</returns>
    Task<Activity?> GetByIdAsync(
        Guid id,
        Expression<Func<Activity, object?>>[] include,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Adds a new activity. Nothing is written to the DB until <see cref="SaveChangesAsync"/> is called.
    /// </summary>
    /// <param name="toAdd">The activity to add.</param>
    void Add(Activity toAdd);

    /// <summary>
    /// Gets the activities that match the predicate, without change tracking.
    /// </summary>
    /// <param name="predicate">The condition every returned activity has to satisfy.</param>
    /// <param name="include">Navigation properties to load together with each activity. Null loads none.</param>
    /// <param name="orderBy">The property to sort by. Null keeps the database order.</param>
    /// <param name="orderByDesc">True to sort descending, false to sort ascending. Only used when <paramref name="orderBy"/> is set.</param>
    /// <param name="skip">How many matching activitys to skip (for paging). Null skips none.</param>
    /// <param name="take">The maximum number of activitys to return (for paging). Null returns all of them.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>The matching activitys, or an empty list when nothing matches.</returns>
    Task<IReadOnlyList<Activity>> FilterAsync(
        Expression<Func<Activity, bool>> predicate,
        Expression<Func<Activity, object?>>[]? include = null,
        Expression<Func<Activity, object?>>? orderBy = null,
        bool orderByDesc = false,
        int? skip = null,
        int? take = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Writes all pending changes to the DB.
    /// </summary>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>True when at least one row was written, otherwise false.</returns>
    Task<bool> SaveChangesAsync(CancellationToken cancellationToken = default);
}
