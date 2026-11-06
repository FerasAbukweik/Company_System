using System.Linq.Expressions;
using HR_System.Core.Domain.Entities;
using HR_System.Core.Enums;

namespace HR_System.Core.Abstraction.RepositoryContracts;

public interface ITasksRepository
{
    /// <summary>
    /// Adds a new task. Nothing is written to the DB until <see cref="SaveChangesAsync"/> is called.
    /// </summary>
    /// <param name="task">The task to add.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests (not used, adding is synchronous).</param>
    void Add(AppTask task, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a task by its id, without change tracking.
    /// </summary>
    /// <param name="taskId">The id of the task.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>The task, or null when it doesn't exist.</returns>
    Task<AppTask?> GetByIdAsync(Guid taskId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a task by its id together with the given navigation properties, without change tracking.
    /// </summary>
    /// <param name="taskId">The id of the task.</param>
    /// <param name="include">Navigation properties to load together with the task.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>The task, or null when it doesn't exist.</returns>
    Task<AppTask?> GetByIdAsync(
        Guid taskId,
        Expression<Func<AppTask, object?>>[] include,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the tasks that match the predicate, without change tracking.
    /// </summary>
    /// <param name="predicate">The condition every returned task has to satisfy.</param>
    /// <param name="include">Navigation properties to load together with each task. Null loads none.</param>
    /// <param name="orderBy">The property to sort by. Null keeps the database order.</param>
    /// <param name="orderByDesc">True to sort descending, false to sort ascending. Only used when <paramref name="orderBy"/> is set.</param>
    /// <param name="skip">How many matching tasks to skip (for paging). Null skips none.</param>
    /// <param name="take">The maximum number of tasks to return (for paging). Null returns all of them.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>The matching tasks, or an empty list when nothing matches.</returns>
    Task<IReadOnlyList<AppTask>> FilterAsync(
        Expression<Func<AppTask, bool>> predicate,
        Expression<Func<AppTask, object?>>[]? include = null,
        Expression<Func<AppTask, object?>>? orderBy = null,
        bool orderByDesc = false,
        int? skip = null,
        int? take = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Finds the task, applies the new status to it and marks only the Status column as modified.
    /// Nothing is written to the DB until <see cref="SaveChangesAsync"/> is called, so the caller can still reject the change.
    /// </summary>
    /// <param name="taskId">The id of the task to update.</param>
    /// <param name="newStatus">The status to apply.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>The updated task, or null when it doesn't exist.</returns>
    Task<AppTask?> UpdateStatusAsync(Guid taskId, TaskStatusEnum newStatus, CancellationToken cancellationToken = default);

    /// <summary>
    /// Writes all pending changes to the DB.
    /// </summary>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>True when at least one row was written, otherwise false.</returns>
    Task<bool> SaveChangesAsync(CancellationToken cancellationToken = default);
}
