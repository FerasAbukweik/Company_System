using HR_System.Core.common;
using HR_System.Core.DTO.Approval;
using HR_System.Core.Enums;

namespace HR_System.Core.Interfaces.ServiceContracts.IApprovalService;

public interface IApprovalUpdateService
{
    Task<Result<ApprovalDTO>> UpdateStatus(Guid approvalId, ApprovalStatusEnum newStatus,Guid currentUserId, CancellationToken cancellationToken = default);
}