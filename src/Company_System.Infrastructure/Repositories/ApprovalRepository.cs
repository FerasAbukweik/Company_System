using HR_System.Core.Domain.Entities;
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
        
        return  toUpdate;
    }

    public void Add(Approval approval)
    {
        dbContext.Approvals.Add(approval);
    }

    public async Task<IReadOnlyList<Approval>> GetNeedsApprovalAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var result = await dbContext.Approvals.Where(a => a.ManagerId == userId)
            .AsNoTracking()
            .ToListAsync(cancellationToken);
        
        return result;
    }

    public async Task<bool> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return (await dbContext.SaveChangesAsync(cancellationToken)) > 0;
    }
     
}