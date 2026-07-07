using HR_System.Core.common;
using HR_System.Core.DTO.Approval;
using HR_System.Core.DTO.Task;
using HR_System.Core.Enums;

namespace HR_System.Core.Interfaces.ServiceContracts;

public interface ITasksApprovalsService
{
    Task<Result<TaskDTO>> UpdateTaskStatusAsync(Guid currentUserId, Guid taskId, TaskStatusEnum newStatus,
        CancellationToken cancellationToken = default);
    Task<Result<RequestedApprovalDTO>> UpdateApprovalStatus(Guid approvalId, ApprovalStatusEnum newStatus,Guid currentUserId, CancellationToken cancellationToken = default);
}