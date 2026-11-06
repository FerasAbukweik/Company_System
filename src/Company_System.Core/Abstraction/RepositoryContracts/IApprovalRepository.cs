using System.Linq.Expressions;
using HR_System.Core.Domain.Entities;
using HR_System.Core.Enums;

namespace HR_System.Core.Abstraction.RepositoryContracts;

public interface IApprovalRepository
{
    /// <summary>
    /// Gets an approval by its id, without change tracking.
    /// </summary>
    /// <param name="approvalId">The id of the approval.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>The approval, or null when it doesn't exist.</returns>
    Task<Approval?> GetByIdAsync(Guid approvalId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets an approval by its id together with the given navigation properties, without change tracking.
    /// </summary>
    /// <param name="approvalId">The id of the approval.</param>
    /// <param name="include">Navigation properties to load together with the approval.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>The approval, or null when it doesn't exist.</returns>
    Task<Approval?> GetByIdAsync(
        Guid approvalId,
        Expression<Func<Approval, object?>>[] include,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Finds the approval, applies the new status to it and marks only the Status column as modified.
    /// Nothing is written to the DB until <see cref="SaveChangesAsync"/> is called, so the caller can still reject the change.
    /// </summary>
    /// <param name="approvalId">The id of the approval to update.</param>
    /// <param name="newStatus">The status to apply.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>The updated approval, or null when it doesn't exist.</returns>
    Task<Approval?> UpdateStatusAsync(Guid approvalId, ApprovalStatusEnum newStatus, CancellationToken cancellationToken = default);

    /// <summary>
    /// Adds a new approval. Nothing is written to the DB until <see cref="SaveChangesAsync"/> is called.
    /// </summary>
    /// <param name="approval">The approval to add.</param>
    void Add(Approval approval);

    /// <summary>
    /// Gets the approvals that match the predicate, without change tracking.
    /// </summary>
    /// <param name="predicate">The condition every returned approval has to satisfy.</param>
    /// <param name="include">Navigation properties to load together with each approval. Null loads none.</param>
    /// <param name="orderBy">The property to sort by. Null keeps the database order.</param>
    /// <param name="orderByDesc">True to sort descending, false to sort ascending. Only used when <paramref name="orderBy"/> is set.</param>
    /// <param name="skip">How many matching approvals to skip (for paging). Null skips none.</param>
    /// <param name="take">The maximum number of approvals to return (for paging). Null returns all of them.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>The matching approvals, or an empty list when nothing matches.</returns>
    Task<IReadOnlyList<Approval>> FilterAsync(
        Expression<Func<Approval, bool>> predicate,
        Expression<Func<Approval, object?>>[]? include = null,
        Expression<Func<Approval, object?>>? orderBy = null,
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
