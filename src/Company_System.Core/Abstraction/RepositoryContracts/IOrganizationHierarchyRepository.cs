using System.Linq.Expressions;
using HR_System.Core.Domain.Entities;
using HR_System.Core.DTO.LazyLoading;
using HR_System.Core.DTO.OrganizationHierarchy;

namespace HR_System.Core.Abstraction.RepositoryContracts;

public interface IOrganizationHierarchyRepository
{
    /// <summary>
    /// Gets a hierarchy node by its id, without change tracking. Inactive (removed) nodes are returned too.
    /// </summary>
    /// <param name="id">The id of the hierarchy node.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>The node, or null when it doesn't exist.</returns>
    Task<OrganizationHierarchy?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a hierarchy node by its id together with the given navigation properties, without change tracking.
    /// Inactive (removed) nodes are returned too.
    /// </summary>
    /// <param name="id">The id of the hierarchy node.</param>
    /// <param name="include">Navigation properties to load together with the node.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>The node, or null when it doesn't exist.</returns>
    Task<OrganizationHierarchy?> GetByIdAsync(
        Guid id,
        Expression<Func<OrganizationHierarchy, object?>>[] include,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Adds a new hierarchy node. Nothing is written to the DB until <see cref="SaveChangesAsync"/> is called.
    /// </summary>
    /// <param name="toAdd">The node to add.</param>
    void Add(OrganizationHierarchy toAdd);

    /// <summary>Stages a manager change; persisted by SaveChangesAsync.</summary>
    void SetParent(Guid id, Guid parentId);

    /// <summary>
    /// Gets the hierarchy nodes that match the predicate, without change tracking.
    /// </summary>
    /// <param name="predicate">The condition every returned node has to satisfy.</param>
    /// <param name="include">Navigation properties to load together with each node. Null loads none.</param>
    /// <param name="orderBy">The property to sort by. Null keeps the database order.</param>
    /// <param name="orderByDesc">True to sort descending, false to sort ascending. Only used when <paramref name="orderBy"/> is set.</param>
    /// <param name="skip">How many matching nodes to skip (for paging). Null skips none.</param>
    /// <param name="take">The maximum number of nodes to return (for paging). Null returns all of them.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>The matching nodes, or an empty list when nothing matches.</returns>
    /// <remarks>Inactive (removed) nodes are not filtered out, add <c>o.IsActive</c> to the predicate to exclude them.</remarks>
    Task<IReadOnlyList<OrganizationHierarchy>> FilterAsync(
        Expression<Func<OrganizationHierarchy, bool>> predicate,
        Expression<Func<OrganizationHierarchy, object?>>[]? include = null,
        Expression<Func<OrganizationHierarchy, object?>>? orderBy = null,
        bool orderByDesc = false,
        int? skip = null,
        int? take = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Finds the node and marks it and its whole branch as inactive (IsActive = false).
    /// Nothing is written to the DB until <see cref="SaveChangesAsync"/> is called, so the caller can still reject the change
    /// (e.g. the root must never be removed).
    /// </summary>
    /// <param name="id">The id of the node to deactivate.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>The deactivated node, or null when it doesn't exist or is already inactive.</returns>
    Task<OrganizationHierarchy?> DeactivateBranchAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Writes all pending changes to the DB.
    /// </summary>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>True when at least one row was written, otherwise false.</returns>
    Task<bool> SaveChangesAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the active hierarchy node of a user, together with its parent node, without change tracking.
    /// </summary>
    /// <param name="userId">The id of the user.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>The user's node, or null when the user isn't in the hierarchy (or was removed from it).</returns>
    Task<OrganizationHierarchy?> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the user ids of all parents above a user (direct manager, their manager, and so on up to the root).
    /// </summary>
    /// <param name="userId">The id of the user whose parents are needed.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>The user ids of all parents of the supplied user, not including the user.</returns>
    Task<IReadOnlyList<Guid>> GetParentUserIds(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the user ids of a user and everyone under him in the hierarchy.
    /// </summary>
    /// <param name="userId">The id of the user at the top of the subtree.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>The user ids of the supplied user and all of his descendants.</returns>
    Task<IReadOnlyList<Guid>> GetSubTreeUserIds(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the tree id and user name of the active hierarchy nodes, one section at a time.
    /// </summary>
    /// <param name="lazyData">How many nodes were already taken and how many to return now.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>The requested section of tree ids with their user names.</returns>
    Task<IReadOnlyList<UserNameDTO>> GetUserNames(LazyDTO lazyData, CancellationToken cancellationToken = default);
}
