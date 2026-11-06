using System.Linq.Expressions;
using HR_System.Core.Abstraction.RepositoryContracts;
using HR_System.Core.Domain.Entities;
using HR_System.Core.Enums;
using Microsoft.EntityFrameworkCore;

namespace HR_System.Infrastructure.Repositories;

public class ApprovalRepository(
    ApplicationDbContext dbContext
    ) : IApprovalRepository
{
    public void Add(Approval approval)
    {
        dbContext.Approvals.Add(approval);
    }

    // read only, so no tracking
    public async Task<Approval?> GetByIdAsync(Guid approvalId, CancellationToken cancellationToken = default)
    {
        return await dbContext.Approvals
            .AsNoTracking()
            .SingleOrDefaultAsync(e => e.Id == approvalId, cancellationToken);
    }

    // read only, so no tracking
    public async Task<Approval?> GetByIdAsync(
        Guid approvalId,
        Expression<Func<Approval, object?>>[] include,
        CancellationToken cancellationToken = default)
    {
        IQueryable<Approval> query = dbContext.Approvals.AsNoTracking();

        foreach (var inc in include)
        {
            query = query.Include(inc);
        }

        return await query.SingleOrDefaultAsync(a => a.Id == approvalId, cancellationToken);
    }

    public async Task<Approval?> UpdateStatusAsync(Guid approvalId, ApprovalStatusEnum newStatus, CancellationToken cancellationToken = default)
    {
        // read only, so no tracking
        var approval = await GetByIdAsync(approvalId, cancellationToken);
        if (approval is null) return null;

        // start tracking it (Unchanged) and mark only Status as modified
        dbContext.Approvals.Attach(approval);
        approval.Status = newStatus;
        dbContext.Entry(approval).Property(a => a.Status).IsModified = true;

        // nothing is written until SaveChangesAsync is called by the service
        return approval;
    }

    public async Task<IReadOnlyList<Approval>> FilterAsync(
        Expression<Func<Approval, bool>> predicate,
        Expression<Func<Approval, object?>>[]? include = null,
        Expression<Func<Approval, object?>>? orderBy = null,
        bool orderByDesc = false,
        int? skip = null,
        int? take = null,
        CancellationToken cancellationToken = default)
    {
        return await Filter(predicate, include, orderBy, orderByDesc, skip, take).ToListAsync(cancellationToken);
    }

    private IQueryable<Approval> Filter(
        Expression<Func<Approval, bool>> predicate,
        Expression<Func<Approval, object?>>[]? include = null,
        Expression<Func<Approval, object?>>? orderBy = null,
        bool orderByDesc = false,
        int? skip = null,
        int? take = null
        )
    {
        var query = dbContext.Approvals.AsNoTracking().Where(predicate);

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
