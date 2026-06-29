using HR_System.Core.common;
using HR_System.Core.DTO.LazyLoading;
using HR_System.Core.DTO.Message;

namespace HR_System.Core.Interfaces.ServiceContracts;

public interface IMessageService
{
    Task<Result<MessageDTO>> AddAsync(MessageAddDTO toAdd, Guid userId, CancellationToken cancellationToken = default);
    Task<Result<IReadOnlyList<MessageDTO>>> LazyGetMessages(Guid userId, LazyDTO lazyData,
        CancellationToken cancellationToken = default);
}