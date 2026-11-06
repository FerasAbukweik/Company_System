using HR_System.Core.DTO.LazyLoading;
using HR_System.Core.DTO.OrganizationHierarchy;
using HR_System.Core.Result;

namespace HR_System.Core.Abstraction.ServiceContracts;

public interface IOrganizationHierarchyService
{
    /// <summary>
    /// Adds a user to the organization hierarchy under a parent and saves it.
    /// </summary>
    /// <param name="toAdd">The user to add and the parent node to add him under.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>A result containing the created hierarchy node.</returns>
    Task<Result<OrganizationHierarchyDTO>> AddAsync(OrganizationHierarchyAddDTO toAdd, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the children of the given parents, with their own children loaded a few layers deep.
    /// </summary>
    /// <param name="parents">The ids of the parent nodes. Null or empty returns the root of the hierarchy.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>A result containing each parent id with its children (the root nodes under <see cref="Guid.Empty"/> when no parents were supplied).</returns>
    Task<Result<IReadOnlyDictionary<Guid, IReadOnlyList<OrganizationHierarchyDTO>>>> GetChildrenAsync(IReadOnlyList<Guid>? parents, CancellationToken cancellationToken = default);

    /// <summary>
    /// Removes a node and its whole branch from the hierarchy (they are kept as inactive for history). The root can't be removed.
    /// </summary>
    /// <param name="toRemoveId">The id of the hierarchy node to remove.</param>
    /// <param name="currUserId">The id of the user removing the node.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>A result containing the removed node, or a failure when it doesn't exist or is the root.</returns>
    Task<Result<OrganizationHierarchyDTO>> RemoveAsync(Guid toRemoveId, Guid currUserId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the user ids of all parents above a user.
    /// </summary>
    /// <param name="userId">The id of the user whose parents are needed.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>A result containing the user ids of all parents of the supplied user.</returns>
    Task<Result<IReadOnlyList<Guid>>> GetParentUserIds(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the tree id and user name of the hierarchy nodes, one section at a time.
    /// </summary>
    /// <param name="lazyData">How many nodes were already taken and how many to return now.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>A result containing the requested section of tree ids with their user names.</returns>
    Task<Result<IReadOnlyList<UserNameDTO>>> GetUserNames(LazyDTO lazyData, CancellationToken cancellationToken = default);
}
