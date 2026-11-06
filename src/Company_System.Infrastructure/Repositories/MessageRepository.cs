using System.Linq.Expressions;
using HR_System.Core.Abstraction.RepositoryContracts;
using HR_System.Core.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace HR_System.Infrastructure.Repositories;

public class MessageRepository(
    ApplicationDbContext dbContext
    ) : IMessageRepository
{
    // read only, so no tracking
    public async Task<Message?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await dbContext.Messages
            .AsNoTracking()
            .SingleOrDefaultAsync(e => e.Id == id, cancellationToken);
    }

    // read only, so no tracking
    public async Task<Message?> GetByIdAsync(
        Guid id,
        Expression<Func<Message, object?>>[] include,
        CancellationToken cancellationToken = default)
    {
        IQueryable<Message> query = dbContext.Messages.AsNoTracking();

        foreach (var inc in include)
        {
            query = query.Include(inc);
        }

        return await query.SingleOrDefaultAsync(e => e.Id == id, cancellationToken);
    }

    public void Add(Message message, CancellationToken cancellationToken = default)
    {
        dbContext.Messages.Add(message);
    }

    public async Task<IReadOnlyList<Message>> FilterAsync(
        Expression<Func<Message, bool>> predicate,
        Expression<Func<Message, object?>>[]? include = null,
        Expression<Func<Message, object?>>? orderBy = null,
        bool orderByDesc = false,
        int? skip = null,
        int? take = null,
        CancellationToken cancellationToken = default)
    {
        return await Filter(predicate, include, orderBy, orderByDesc, skip, take).ToListAsync(cancellationToken);
    }

    // messages are only read here, never edited, so no tracking
    private IQueryable<Message> Filter(
        Expression<Func<Message, bool>> predicate,
        Expression<Func<Message, object?>>[]? include = null,
        Expression<Func<Message, object?>>? orderBy = null,
        bool orderByDesc = false,
        int? skip = null,
        int? take = null
        )
    {
        var query = dbContext.Messages.AsNoTracking().Where(predicate);

        if (include != null)
        {
            foreach (var inc in include)
            {
                query = query.Include(inc);
            }
        }

        if (orderBy != null)
        {
            if (orderByDesc) query = query.OrderByDescending(orderBy);
            else query = query.OrderBy(orderBy);

            // messages can share a CreatedAt, Id keeps the paging order stable between sections
            query = ((IOrderedQueryable<Message>)query).ThenByDescending(m => m.Id);
        }

        if (skip != null) query = query.Skip(skip.Value);
        if (take != null) query = query.Take(take.Value);

        return query;
    }

    public async Task<bool> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return await dbContext.SaveChangesAsync(cancellationToken) > 0;
    }
}
