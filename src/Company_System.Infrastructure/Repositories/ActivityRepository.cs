using System.Linq.Expressions;
using HR_System.Core.Abstraction.RepositoryContracts;
using HR_System.Core.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace HR_System.Infrastructure.Repositories;

public class ActivityRepository(
    ApplicationDbContext dbContext
    ) : IActivityRepository
{
    // read only, so no tracking
    public async Task<Activity?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await dbContext.Activities
            .AsNoTracking()
            .SingleOrDefaultAsync(e => e.Id == id, cancellationToken);
    }

    // read only, so no tracking
    public async Task<Activity?> GetByIdAsync(
        Guid id,
        Expression<Func<Activity, object?>>[] include,
        CancellationToken cancellationToken = default)
    {
        IQueryable<Activity> query = dbContext.Activities.AsNoTracking();

        foreach (var inc in include)
        {
            query = query.Include(inc);
        }

        return await query.SingleOrDefaultAsync(e => e.Id == id, cancellationToken);
    }

    public void Add(Activity toAdd)
    {
        dbContext.Activities.Add(toAdd);
    }

    public async Task<IReadOnlyList<Activity>> FilterAsync(
        Expression<Func<Activity, bool>> predicate,
        Expression<Func<Activity, object?>>[]? include = null,
        Expression<Func<Activity, object?>>? orderBy = null,
        bool orderByDesc = false,
        int? skip = null,
        int? take = null,
        CancellationToken cancellationToken = default)
    {
        return await Filter(predicate, include, orderBy, orderByDesc, skip, take).ToListAsync(cancellationToken);
    }

    private IQueryable<Activity> Filter(
        Expression<Func<Activity, bool>> predicate,
        Expression<Func<Activity, object?>>[]? include = null,
        Expression<Func<Activity, object?>>? orderBy = null,
        bool orderByDesc = false,
        int? skip = null,
        int? take = null
        )
    {
        var query = dbContext.Activities.AsNoTracking().Where(predicate);

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
