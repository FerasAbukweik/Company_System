using HR_System.Core.Domain.Entities;
using HR_System.Core.DTO.LazyLoading;
using HR_System.Core.Interfaces.RepositoryContracts;
using Microsoft.EntityFrameworkCore;

namespace HR_System.Infrastructure.Repositories;

public class ActivityRepository(ApplicationDbContext dbContext) : IActivityRepository
{
    public void Add(Activity toAdd)
    {
        dbContext.Activities.Add(toAdd);
    }

    public async Task<IReadOnlyList<Activity>> LazyGetAllSortedAsync(LazyDTO lazyData, CancellationToken cancellationToken = default)
    {
        return await dbContext.Activities
            .OrderByDescending(a => a.CreatedAt)
            .Include(a => a.Task)
            .Include(a => a.Approval)
            .Skip(lazyData.Taken)
            .Take(lazyData.SectionSize)
            .AsNoTracking()
            .ToListAsync(cancellationToken);
    }

    public async Task<bool> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return (await dbContext.SaveChangesAsync(cancellationToken)) > 0;
    }
}