using System.Collections.Immutable;
using HR_System.Core.common;
using HR_System.Core.Domain.Entities;
using HR_System.Core.DTO.LazyLoading;
using HR_System.Core.DTO.Message;
using HR_System.Core.Interfaces.RepositoryContracts;
using HR_System.Core.Interfaces.ServiceContracts;

namespace HR_System.Infrastructure.Services;

public class MessageService(IMessageRepository messageRepository) : IMessageService
{
    public async Task<Result<MessageDTO>> AddAsync(MessageAddDTO toAdd, Guid userId, CancellationToken cancellationToken = default)
    {
        var toAdd_DB = new Message()
        {
            Content = toAdd.Content,
            ReceiverId = toAdd.ReceiverId,
            SenderId = userId,
        };
        messageRepository.Add(toAdd_DB);

        if (!(await messageRepository.SaveChangesAsync(cancellationToken)))
            return Result<MessageDTO>.Failure("Failed to save changes to DB");

        return Result<MessageDTO>.Success(toAdd_DB.ToDTO(userId));
    }

    public async Task<Result<IReadOnlyList<MessageDTO>>> LazyGetMessages(Guid userId, LazyDTO lazyData, CancellationToken cancellationToken = default)
    {
        var messages = await messageRepository.LazyGetMessages(userId, lazyData, cancellationToken);

        return Result<IReadOnlyList<MessageDTO>>.Success(messages.Select(m => m.ToDTO(userId)).ToImmutableList());
    }
}