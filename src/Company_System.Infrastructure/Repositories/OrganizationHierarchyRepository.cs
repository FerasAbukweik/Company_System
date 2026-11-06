using System.Linq.Expressions;
using HR_System.Core.Abstraction.RepositoryContracts;
using HR_System.Core.Domain.Entities;
using HR_System.Core.DTO.LazyLoading;
using HR_System.Core.DTO.OrganizationHierarchy;
using HR_System.Infrastructure.ExtensionMethods;
using Microsoft.EntityFrameworkCore;

namespace HR_System.Infrastructure.Repositories;

public class OrganizationHierarchyRepository(
    ApplicationDbContext dbContext
    ) : IOrganizationHierarchyRepository
{
    // read only, so no tracking
    public async Task<OrganizationHierarchy?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await dbContext.OrganizationHierarchies
            .AsNoTracking()
            .SingleOrDefaultAsync(e => e.Id == id, cancellationToken);
    }

    // read only, so no tracking
    public async Task<OrganizationHierarchy?> GetByIdAsync(
        Guid id,
        Expression<Func<OrganizationHierarchy, object?>>[] include,
        CancellationToken cancellationToken = default)
    {
        IQueryable<OrganizationHierarchy> query = dbContext.OrganizationHierarchies.AsNoTracking();

        foreach (var inc in include)
        {
            query = query.Include(inc);
        }

        return await query.SingleOrDefaultAsync(e => e.Id == id, cancellationToken);
    }

    public void SetParent(Guid id, Guid parentId)
    {
        var node = dbContext.OrganizationHierarchies.Local.FirstOrDefault(node => node.Id == id)
            ?? dbContext.OrganizationHierarchies.Attach(new OrganizationHierarchy { Id = id, UserId = Guid.Empty }).Entity;

        node.ParentId = parentId;
        dbContext.Entry(node).Property(node => node.ParentId).IsModified = true;
    }

    public void Add(OrganizationHierarchy toAdd)
    {
        dbContext.OrganizationHierarchies.Add(toAdd);
    }

    public async Task<IReadOnlyList<OrganizationHierarchy>> FilterAsync(
        Expression<Func<OrganizationHierarchy, bool>> predicate,
        Expression<Func<OrganizationHierarchy, object?>>[]? include = null,
        Expression<Func<OrganizationHierarchy, object?>>? orderBy = null,
        bool orderByDesc = false,
        int? skip = null,
        int? take = null,
        CancellationToken cancellationToken = default)
    {
        return await Filter(predicate, include, orderBy, orderByDesc, skip, take).ToListAsync(cancellationToken);
    }

    public async Task<OrganizationHierarchy?> DeactivateBranchAsync(Guid id, CancellationToken cancellationToken = default)
    {
        // collect the node and all of its descendants, level by level
        var branchIds = new List<Guid> { id };
        var pending = new List<Guid> { id };
        while (pending.Count > 0)
        {
            pending = await dbContext.OrganizationHierarchies
                .AsNoTracking()
                .Where(o => o.IsActive && o.ParentId != null && pending.Contains(o.ParentId.Value))
                .Select(o => o.Id)
                .ToListAsync(cancellationToken);

            branchIds.AddRange(pending);
        }

        // read only, so no tracking
        var branch = await dbContext.OrganizationHierarchies
            .AsNoTracking()
            .Where(o => o.IsActive && branchIds.Contains(o.Id))
            .ToListAsync(cancellationToken);

        var node = branch.SingleOrDefault(o => o.Id == id);
        if (node is null) return null;

        // start tracking each node (Unchanged) and mark only IsActive as modified
        foreach (var n in branch)
        {
            dbContext.OrganizationHierarchies.Attach(n);
            n.IsActive = false;
            dbContext.Entry(n).Property(o => o.IsActive).IsModified = true;
        }

        // nothing is written until SaveChangesAsync is called by the service
        return node;
    }

    public async Task<OrganizationHierarchy?> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        // read-only, the parent is needed so it is included
        return await dbContext.OrganizationHierarchies
            .AsNoTracking()
            .Include(o => o.Parent)
            .SingleOrDefaultAsync(o => o.IsActive && o.UserId == userId, cancellationToken);
    }

    public async Task<IReadOnlyList<Guid>> GetParentUserIds(Guid userId, CancellationToken cancellationToken = default)
    {
        return await dbContext.GetFatherUserIds(userId).ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Guid>> GetSubTreeUserIds(Guid userId, CancellationToken cancellationToken = default)
    {
        return await dbContext.GetSubTreeUserIds(userId).ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<UserNameDTO>> GetUserNames(LazyDTO lazyData, CancellationToken cancellationToken = default)
    {
        return await Filter(
            o => o.IsActive,
            null,
            o => o.Id,
            false,
            lazyData.Taken,
            lazyData.SectionSize
            )
            .Select(oh => new UserNameDTO()
            {
                TreeId = oh.Id,
                UserName = oh.User!.UserName!
            }).ToListAsync(cancellationToken);
    }

    private IQueryable<OrganizationHierarchy> Filter(
        Expression<Func<OrganizationHierarchy, bool>> predicate,
        Expression<Func<OrganizationHierarchy, object?>>[]? include = null,
        Expression<Func<OrganizationHierarchy, object?>>? orderBy = null,
        bool orderByDesc = false,
        int? skip = null,
        int? take = null
        )
    {
        var query = dbContext.OrganizationHierarchies.AsNoTracking().Where(predicate);

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