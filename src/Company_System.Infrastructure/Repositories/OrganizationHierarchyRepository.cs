using HR_System.Core.Domain.Entities;
using HR_System.Core.Interfaces.RepositoryContracts;
using Microsoft.EntityFrameworkCore;

namespace HR_System.Infrastructure.Repositories;

public class OrganizationHierarchyRepository(ApplicationDbContext dbContext) : IOrganizationHierarchyRepository
{
    public void Add(OrganizationHierarchy toAdd)
    {
        dbContext.OrganizationHierarchies.Add(toAdd);
    }

    public async Task<IReadOnlyList<OrganizationHierarchy>> GetChildrenAsync(IReadOnlyList<Guid>? parents, CancellationToken cancellationToken = default)
    {
        var query = dbContext.OrganizationHierarchies.AsQueryable();
        
        // layer 1
        if(parents == null || parents.Count == 0)
            query = query.Where(o => o.ParentId == null);
        else
            query = query.Where(o => o.ParentId != null && parents.Contains(o.ParentId.Value));
        
        
        // 5 layers is the default section size
        query = query
            .Include(o => o.Children) // layer 2
            .ThenInclude(c => c.Children)  // layer 3
            .ThenInclude(c => c.Children)  // layer 4
            .ThenInclude(c => c.Children); // layer5
        
        return await query.AsNoTracking().ToListAsync(cancellationToken);
    }

    public async Task<OrganizationHierarchy?> RemoveAsync(Guid toRemoveId, CancellationToken cancellationToken = default)
    {
        var toRemove = await dbContext.OrganizationHierarchies
            .AsNoTracking()
            .SingleOrDefaultAsync(o => o.Id == toRemoveId, cancellationToken);

        if (toRemove == null)
            return toRemove;
        
        dbContext.OrganizationHierarchies.Remove(toRemove);

        return toRemove;
    }

    public async Task<bool> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return (await dbContext.SaveChangesAsync(cancellationToken)) > 0;
    }
}