using HR_System.Core.Abstraction.ServiceContracts;
using HR_System.Core.DTO.LazyLoading;
using HR_System.Core.DTO.Message;
using HR_System.Core.Result;
using HR_System.Infrastructure.Services;
using Microsoft.Extensions.Logging;

namespace HR_System.Infrastructure.Decorators.Loggers;

public class LoggingMessagesService(
    IMessagesService inner,
    ILogger<LoggingMessagesService> logger) : IMessagesService
{
    private const string ServiceName = nameof(MessagesService);

    public async Task<Result<MessageDTO>> AddAsync(MessageAddDTO toAdd, Guid userId, CancellationToken cancellationToken = default)
    {
        var result = await inner.AddAsync(toAdd, userId, cancellationToken);
        // the message content is deliberately not logged
        logger.LogResult(result, ServiceName, nameof(AddAsync),
            $"SenderId: {userId}, ReceiverId: {toAdd.ReceiverId}, MessageId: {result.Value?.Id}");
        return result;
    }

    public async Task<Result<IReadOnlyList<MessageDTO>>> LazyGetMessages(Guid userId, Guid otherUserId, LazyDTO lazyData, CancellationToken cancellationToken = default)
    {
        var result = await inner.LazyGetMessages(userId, otherUserId, lazyData, cancellationToken);
        logger.LogResult(result, ServiceName, nameof(LazyGetMessages),
            $"UserId: {userId}, OtherUserId: {otherUserId}", logSuccess: false);
        return result;
    }
}
