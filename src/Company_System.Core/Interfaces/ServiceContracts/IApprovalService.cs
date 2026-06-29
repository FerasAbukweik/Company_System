using HR_System.Core.common;
using HR_System.Core.DTO.Approval;
using HR_System.Core.Enums;

namespace HR_System.Core.Interfaces.ServiceContracts;

public interface IApprovalService
{
    Task<Result<IReadOnlyList<ApprovalDTO>>> GetNeedsApprovalAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<Result<ApprovalDTO>> AddAsync(ApprovalAddDTO toAddApproval,Guid userId, CancellationToken cancellationToken = default);
    Task<Result<ApprovalDTO>> UpdateStatus(Guid approvalId, ApprovalStatusEnum newStatus,Guid currentUserId, CancellationToken cancellationToken = default);
    
}