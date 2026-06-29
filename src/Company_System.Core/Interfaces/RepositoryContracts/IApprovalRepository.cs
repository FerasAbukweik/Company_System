using HR_System.Core.Domain.Entities;
using HR_System.Core.Enums;

namespace HR_System.Core.Interfaces.RepositoryContracts;

public interface IApprovalRepository
{
    Task<Approval?> UpdateStatus(Guid approvalId, ApprovalStatusEnum newStatus, CancellationToken  cancellationToken = default);
    void Add(Approval approval);
    Task<IReadOnlyList<Approval>> GetNeedsApprovalAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<bool> SaveChangesAsync(CancellationToken cancellationToken = default);
}