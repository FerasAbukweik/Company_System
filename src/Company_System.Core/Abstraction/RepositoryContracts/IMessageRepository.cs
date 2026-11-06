using System.Linq.Expressions;
using HR_System.Core.Domain.Entities;

namespace HR_System.Core.Abstraction.RepositoryContracts;

public interface IMessageRepository
{
    /// <summary>
    /// Gets a message by its id, without change tracking.
    /// </summary>
    /// <param name="id">The id of the message.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>The message, or null when it doesn't exist.</returns>
    Task<Message?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a message by its id together with the given navigation properties, without change tracking.
    /// </summary>
    /// <param name="id">The id of the message.</param>
    /// <param name="include">Navigation properties to load together with the message.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>The message, or null when it doesn't exist.</returns>
    Task<Message?> GetByIdAsync(
        Guid id,
        Expression<Func<Message, object?>>[] include,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Adds a new message. Nothing is written to the DB until <see cref="SaveChangesAsync"/> is called.
    /// </summary>
    /// <param name="message">The message to add.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests (not used, adding is synchronous).</param>
    void Add(Message message, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the messages that match the predicate, without change tracking.
    /// </summary>
    /// <param name="predicate">The condition every returned message has to satisfy.</param>
    /// <param name="include">Navigation properties to load together with each message. Null loads none.</param>
    /// <param name="orderBy">The property to sort by. Null keeps the database order.</param>
    /// <param name="orderByDesc">True to sort descending, false to sort ascending. Only used when <paramref name="orderBy"/> is set.</param>
    /// <param name="skip">How many matching messages to skip (for paging). Null skips none.</param>
    /// <param name="take">The maximum number of messages to return (for paging). Null returns all of them.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>The matching messages, or an empty list when nothing matches.</returns>
    Task<IReadOnlyList<Message>> FilterAsync(
        Expression<Func<Message, bool>> predicate,
        Expression<Func<Message, object?>>[]? include = null,
        Expression<Func<Message, object?>>? orderBy = null,
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
