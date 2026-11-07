using System.Linq.Expressions;
using HR_System.Core.Abstraction.RepositoryContracts;
using HR_System.Core.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace HR_System.Infrastructure.Repositories;

public class NotificationsRepository(
    ApplicationDbContext dbContext
    ) : INotificationsRepository
{
    public void Add(Notification notification, CancellationToken cancellationToken = default)
    {
        dbContext.Notifications.Add(notification);
    }

    public async Task<IReadOnlyList<Notification>> FilterAsync(
        Expression<Func<Notification, bool>> predicate,
        Expression<Func<Notification, object?>>[]? include = null,
        Expression<Func<Notification, object?>>? orderBy = null,
        bool orderByDesc = false,
        int? skip = null,
        int? take = null,
        CancellationToken cancellationToken = default)
    {
        return await Filter(predicate, include, orderBy, orderByDesc, skip, take).ToListAsync(cancellationToken);
    }

    // notifications are only read here, never edited, so no tracking
    private IQueryable<Notification> Filter(
        Expression<Func<Notification, bool>> predicate,
        Expression<Func<Notification, object?>>[]? include = null,
        Expression<Func<Notification, object?>>? orderBy = null,
        bool orderByDesc = false,
        int? skip = null,
        int? take = null
        )
    {
        var query = dbContext.Notifications.AsNoTracking().Where(predicate);

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

            // notifications can share a CreatedAt, Id keeps the paging order stable between sections
            query = ((IOrderedQueryable<Notification>)query).ThenByDescending(m => m.Id);
        }

        if (skip != null) query = query.Skip(skip.Value);
        if (take != null) query = query.Take(take.Value);

        return query;
    }

    public Task<int> GetUnreadCountAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        return dbContext.Notifications.CountAsync(notification => notification.UserId == userId &&
            !notification.IsRead, cancellationToken);
    }

    public void SetRead(Notification notification)
    {
        dbContext.Notifications.Attach(notification);
        notification.IsRead = true;
        dbContext.Entry(notification).Property(item => item.IsRead).IsModified = true;
    }

    public async Task<bool> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return await dbContext.SaveChangesAsync(cancellationToken) > 0;
    }
}
