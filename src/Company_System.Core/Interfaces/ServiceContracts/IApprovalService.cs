using HR_System.Core.common;
using HR_System.Core.DTO.Approval;
using HR_System.Core.DTO.LazyLoading;
using HR_System.Core.Enums;

namespace HR_System.Core.Interfaces.ServiceContracts;

public interface IApprovalService
{
    Task<Result<IReadOnlyList<ToApproveDTO>>> GetNeedsApprovalAsync(LazyDTO lazyData, Guid userId, CancellationToken cancellationToken = default);
    Task<Result<IReadOnlyList<RequestedApprovalDTO>>> GetRequested(LazyDTO lazyData, Guid userId, CancellationToken cancellationToken = default);
    Task<Result<RequestedApprovalDTO>> UpdateStatus(Guid approvalId, ApprovalStatusEnum newStatus,Guid currentUserId, CancellationToken  cancellationToken = default);
    Task<Result<ToApproveDTO>> AddAsync(ApprovalAddDTO toAddApproval,Guid userId,Guid managerUserId, CancellationToken cancellationToken = default);
    Task<Result<ToApproveDTO>> RequestHoliday(Guid currUserId ,CancellationToken cancellationToken = default);
}