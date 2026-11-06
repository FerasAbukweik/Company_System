using System.Net;
using System.Collections.Immutable;
using HR_System.Core.Abstraction.RepositoryContracts;
using HR_System.Core.Abstraction.ServiceContracts;
using HR_System.Core.Domain.Entities;
using HR_System.Core.DTO.LazyLoading;
using HR_System.Core.DTO.Message;
using HR_System.Core.Result;

namespace HR_System.Infrastructure.Services;

public class MessagesService(
    IMessageRepository messageRepository) : IMessagesService
{
    public async Task<Result<MessageDTO>> AddAsync(MessageAddDTO toAdd, Guid userId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(toAdd.Content) || toAdd.Content.Length > 500 || toAdd.ReceiverId == Guid.Empty)
            return Result<MessageDTO>.Failure("A recipient and message of 1 to 500 characters are required", HttpStatusCode.BadRequest);

        var toAdd_DB = new Message()
        {
            Content = toAdd.Content,
            ReceiverId = toAdd.ReceiverId,
            SenderId = userId,
        };
        messageRepository.Add(toAdd_DB, cancellationToken);

        if (!(await messageRepository.SaveChangesAsync(cancellationToken)))
        {
            return Result<MessageDTO>.Failure("Failed to save changes to DB");
        }


        return Result<MessageDTO>.Success(toAdd_DB.ToDTO(userId));
    }

    public async Task<Result<IReadOnlyList<MessageDTO>>> LazyGetMessages(Guid userId, Guid otherUserId, LazyDTO lazyData, CancellationToken cancellationToken = default)
    {
        // the conversation between the two users, newest first, one section at a time
        var messages = await messageRepository.FilterAsync(
            m => (m.SenderId == userId && m.ReceiverId == otherUserId) ||
                 (m.SenderId == otherUserId && m.ReceiverId == userId),
            orderBy: m => m.CreatedAt,
            orderByDesc: true,
            skip: lazyData.Taken,
            take: lazyData.SectionSize,
            cancellationToken: cancellationToken);

        return Result<IReadOnlyList<MessageDTO>>.Success(messages.Select(m => m.ToDTO(userId)).ToImmutableList());
    }
}