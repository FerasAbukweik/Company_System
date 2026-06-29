using HR_System.Core.Domain.Entities;
using HR_System.Core.DTO.LazyLoading;

namespace HR_System.Core.Interfaces.RepositoryContracts;

public interface IMessageRepository
{
    void Add(Message message, CancellationToken cancellationToken = default);
    
    Task<IReadOnlyList<Message>> LazyGetMessages(Guid userId, LazyDTO lazyData, CancellationToken cancellationToken = default);
    Task<bool> SaveChangesAsync(CancellationToken cancellationToken = default);
}