using System.Linq.Expressions;
using HR_System.Core.Domain.Entities;
using HR_System.Core.DTO.LazyLoading;
using HR_System.Core.Enums;
using HR_System.Core.Interfaces.RepositoryContracts;
using Microsoft.EntityFrameworkCore;

namespace HR_System.Infrastructure.Repositories;

public class ApprovalRepository(ApplicationDbContext dbContext) : IApprovalRepository
{
    public async Task<Approval?> UpdateStatus(Guid approvalId, ApprovalStatusEnum newStatus, CancellationToken cancellationToken = default)
    {
        var toUpdate = await dbContext.Approvals.SingleOrDefaultAsync(a => a.Id == approvalId, cancellationToken);
        if (toUpdate == null)
            return null;

        toUpdate.Status = newStatus;
        
        return toUpdate;
    }

    public void Add(Approval approval)
    {
        dbContext.Approvals.Add(approval);
    }

    public async Task<IReadOnlyList<Approval>> FilterAsync(Expression<Func<Approval, bool>> filter,Expression<Func<Approval, object>>[]? include = null, CancellationToken cancellationToken = default)
    {
        var query = dbContext.Approvals.AsQueryable();

        if (include != null)
        {
            foreach (var inc in include)
            {
                query = query.Include(inc);
            }
        }

        return await query
            .Where(filter)
            .AsNoTracking()
            .ToListAsync(cancellationToken); 
    }
    
    public async Task<IReadOnlyList<Approval>> LazyGetApprovals(LazyDTO lazyData, 
        Expression<Func<Approval, bool>> filter,Expression<Func<Approval, object>>[]? include = null,
        CancellationToken cancellationToken = default)
    {
        var query = dbContext.Approvals.AsQueryable();

        if (include != null)
        {
            foreach (var inc in include)
            {
                query = query.Include(inc);
            }
        }

        return await query
            .Where(filter)
            .OrderByDescending(a => a.CreatedOn)
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