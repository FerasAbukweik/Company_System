using System.Linq.Expressions;
using HR_System.Core.Domain.Entities;
using HR_System.Core.DTO.LazyLoading;
using HR_System.Core.Enums;

namespace HR_System.Core.Interfaces.RepositoryContracts;

public interface IApprovalRepository
{
    Task<Approval?> UpdateStatus(Guid approvalId, ApprovalStatusEnum newStatus, CancellationToken  cancellationToken = default);
    void Add(Approval approval);

    Task<IReadOnlyList<Approval>> FilterAsync(Expression<Func<Approval, bool>> filter,
        Expression<Func<Approval, object>>[]? include = null, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Approval>> LazyGetApprovals(LazyDTO lazyData,
        Expression<Func<Approval, bool>> filter, Expression<Func<Approval, object>>[]? include = null,
        CancellationToken cancellationToken = default);
    Task<bool> SaveChangesAsync(CancellationToken cancellationToken = default);
}