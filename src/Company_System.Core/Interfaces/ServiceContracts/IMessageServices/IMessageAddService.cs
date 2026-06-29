using HR_System.Core.common;
using HR_System.Core.DTO.Message;

namespace HR_System.Core.Interfaces.ServiceContracts.IMessageServices;

public interface IMessageAddService
{
    Task<Result<MessageDTO>> AddAsync(MessageAddDTO toAdd, Guid userId, CancellationToken cancellationToken = default);
}