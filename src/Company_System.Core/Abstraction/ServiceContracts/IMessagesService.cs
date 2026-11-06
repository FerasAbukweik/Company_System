using HR_System.Core.DTO.LazyLoading;
using HR_System.Core.DTO.Message;
using HR_System.Core.Result;

namespace HR_System.Core.Abstraction.ServiceContracts;

public interface IMessagesService
{
    /// <summary>
    /// Saves a message sent from one user to another.
    /// </summary>
    /// <param name="toAdd">The content (1 to 500 characters) and the receiver of the message.</param>
    /// <param name="userId">The id of the sender.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>A result containing the saved message, or a failure when the content or receiver is invalid.</returns>
    Task<Result<MessageDTO>> AddAsync(MessageAddDTO toAdd, Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the conversation between two users, newest first, one section at a time.
    /// </summary>
    /// <param name="userId">The id of the current user.</param>
    /// <param name="otherUserId">The id of the user on the other side of the conversation.</param>
    /// <param name="lazyData">How many messages were already taken and how many to return now.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>A result containing the requested section of messages.</returns>
    Task<Result<IReadOnlyList<MessageDTO>>> LazyGetMessages(Guid userId, Guid otherUserId, LazyDTO lazyData,
        CancellationToken cancellationToken = default);
}
