using HR_System.Core.Domain.Entities;
using HR_System.Core.DTO.LazyLoading;
using HR_System.Core.Interfaces.RepositoryContracts;
using Microsoft.EntityFrameworkCore;

namespace HR_System.Infrastructure.Repositories;

public class MessageRepository(ApplicationDbContext dbContext) : IMessageRepository
{
    public void Add(Message message, CancellationToken cancellationToken = default)
    {
        dbContext.Messages.Add(message);
    }

    public async Task<IReadOnlyList<Message>> LazyGetMessages(Guid userId, LazyDTO lazyData, CancellationToken cancellationToken = default)
    {
        return await dbContext.Messages
            .Where(m => (m.SenderId == userId || m.ReceiverId == userId))
            .OrderByDescending(m => m.CreatedAt)
            .Skip(lazyData.Taken)
            .Take(lazyData.SectionSize)
            .ToListAsync(cancellationToken);
    }

    public async Task<bool> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return (await dbContext.SaveChangesAsync(cancellationToken)) > 0;
    }
}