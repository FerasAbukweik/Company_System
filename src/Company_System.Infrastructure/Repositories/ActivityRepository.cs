using HR_System.Core.Domain.Entities;
using HR_System.Core.DTO.LazyLoading;
using HR_System.Core.Interfaces.RepositoryContracts;
using HR_System.Infrastructure.extensionMethods;
using Microsoft.EntityFrameworkCore;

namespace HR_System.Infrastructure.Repositories;

public class ActivityRepository(ApplicationDbContext dbContext) : IActivityRepository
{
    public void Add(Activity toAdd)
    {
        dbContext.Activities.Add(toAdd);
    }

    public async Task<IReadOnlyList<Activity>> LazyGetAllSortedAsync(LazyDTO lazyData, Guid userId, CancellationToken cancellationToken = default)
    {
        // get users under currUser subtree ids
        var subTreeUserIds = await dbContext.GetSubTreeUserIds(userId).ToListAsync(cancellationToken);

        return await dbContext.Activities
                // get activities which where triggered by users under curr user subtree
            .Where(a => subTreeUserIds.Contains(a.TriggeredById))
            
                // for lazy loading
            .OrderByDescending(a => a.CreatedAt)
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