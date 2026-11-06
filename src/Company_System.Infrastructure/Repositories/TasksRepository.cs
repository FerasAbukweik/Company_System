using System.Linq.Expressions;
using HR_System.Core.Abstraction.RepositoryContracts;
using HR_System.Core.Domain.Entities;
using HR_System.Core.Enums;
using Microsoft.EntityFrameworkCore;

namespace HR_System.Infrastructure.Repositories;

public class TasksRepository(
    ApplicationDbContext dbContext
    ) : ITasksRepository
{
    public void Add(AppTask task, CancellationToken cancellationToken = default)
    {
        dbContext.Tasks.Add(task);
    }

    // read only, so no tracking
    public async Task<AppTask?> GetByIdAsync(Guid taskId, CancellationToken cancellationToken = default)
    {
        return await dbContext.Tasks
            .AsNoTracking()
            .SingleOrDefaultAsync(e => e.Id == taskId, cancellationToken);
    }

    // read only, so no tracking
    public async Task<AppTask?> GetByIdAsync(
        Guid taskId,
        Expression<Func<AppTask, object?>>[] include,
        CancellationToken cancellationToken = default)
    {
        IQueryable<AppTask> query = dbContext.Tasks.AsNoTracking();

        foreach (var inc in include)
        {
            query = query.Include(inc);
        }

        return await query.SingleOrDefaultAsync(t => t.Id == taskId, cancellationToken);
    }

    public async Task<IReadOnlyList<AppTask>> FilterAsync(
        Expression<Func<AppTask, bool>> predicate,
        Expression<Func<AppTask, object?>>[]? include = null,
        Expression<Func<AppTask, object?>>? orderBy = null,
        bool orderByDesc = false,
        int? skip = null,
        int? take = null,
        CancellationToken cancellationToken = default)
    {
        return await Filter(predicate, include, orderBy, orderByDesc, skip, take).ToListAsync(cancellationToken);
    }

    public async Task<AppTask?> UpdateStatusAsync(Guid taskId, TaskStatusEnum newStatus, CancellationToken cancellationToken = default)
    {
        // read only, so no tracking
        var task = await GetByIdAsync(taskId, cancellationToken);
        if (task is null) return null;

        // start tracking it (Unchanged) and mark only Status as modified
        dbContext.Tasks.Attach(task);
        task.Status = newStatus;
        dbContext.Entry(task).Property(t => t.Status).IsModified = true;

        // nothing is written until SaveChangesAsync is called by the service
        return task;
    }

    private IQueryable<AppTask> Filter(
        Expression<Func<AppTask, bool>> predicate,
        Expression<Func<AppTask, object?>>[]? include = null,
        Expression<Func<AppTask, object?>>? orderBy = null,
        bool orderByDesc = false,
        int? skip = null,
        int? take = null
        )
    {
        var query = dbContext.Tasks.AsNoTracking().Where(predicate);

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
