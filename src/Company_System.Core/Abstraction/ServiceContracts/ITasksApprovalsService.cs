using HR_System.Core.DTO.Approval;
using HR_System.Core.DTO.Task;
using HR_System.Core.Enums;
using HR_System.Core.Result;

namespace HR_System.Core.Abstraction.ServiceContracts;

public interface ITasksApprovalsService
{
    /// <summary>
    /// Changes the status of a task. When the task is completed, an approval for it is created for the manager.
    /// </summary>
    /// <param name="currentUserId">The id of the user making the change.</param>
    /// <param name="taskId">The id of the task to update.</param>
    /// <param name="newStatus">The status to apply.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>A result containing the updated task, or a failure when the user isn't allowed to make this change.</returns>
    Task<Result<TaskDTO>> UpdateTaskStatusAsync(Guid currentUserId, Guid taskId, TaskStatusEnum newStatus,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Changes the status of an approval. When a task approval is rejected, the task is set to rejected too.
    /// Only the manager of the approval may do this.
    /// </summary>
    /// <param name="approvalId">The id of the approval to update.</param>
    /// <param name="newStatus">The status to apply.</param>
    /// <param name="currentUserId">The id of the user making the change.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>A result containing the updated approval, or a failure when it doesn't exist or the user isn't its manager.</returns>
    Task<Result<RequestedApprovalDTO>> UpdateApprovalStatus(Guid approvalId, ApprovalStatusEnum newStatus, Guid currentUserId, CancellationToken cancellationToken = default);
}
