using System.Collections.Immutable;
using HR_System.Core.common;
using HR_System.Core.Domain.Entities;
using HR_System.Core.DTO.LazyLoading;
using HR_System.Core.DTO.Message;
using HR_System.Core.Interfaces.RepositoryContracts;
using HR_System.Core.Interfaces.ServiceContracts;
using Microsoft.Extensions.Logging;

namespace HR_System.Infrastructure.Services;

public class MessagesService(
    IMessageRepository messageRepository,
    ILogger<MessagesService> logger) : IMessagesService
{
    public async Task<Result<MessageDTO>> AddAsync(MessageAddDTO toAdd, Guid userId, CancellationToken cancellationToken = default)
    {
        var toAdd_DB = new Message()
        {
            Content = toAdd.Content,
            ReceiverId = toAdd.ReceiverId,
            SenderId = userId,
        };
        messageRepository.Add(toAdd_DB, cancellationToken);

        if (!(await messageRepository.SaveChangesAsync(cancellationToken)))
        {
            logger.LogError("{serviceName}.{methodName} failed saving changes to DB",
                nameof(MessagesService), nameof(AddAsync));
            return Result<MessageDTO>.Failure("Failed to save changes to DB");
        }
        
        logger.LogError("{serviceName}.{methodName} message with id of {messageId} was added",
            nameof(MessagesService), nameof(AddAsync), toAdd_DB.Id);
        
        return Result<MessageDTO>.Success(toAdd_DB.ToDTO(userId));
    }

    public async Task<Result<IReadOnlyList<MessageDTO>>> LazyGetMessages(Guid userId,Guid otherUserId, LazyDTO lazyData, CancellationToken cancellationToken = default)
    {
        var messages = await messageRepository.LazyGetMessages(userId,otherUserId, lazyData, cancellationToken);

        return Result<IReadOnlyList<MessageDTO>>.Success(messages.Select(m => m.ToDTO(userId)).ToImmutableList());
    }
}