using HR_System.Core.DTO.Approval;
using HR_System.Core.DTO.LazyLoading;
using HR_System.Core.Enums;
using HR_System.Core.Result;

namespace HR_System.Core.Abstraction.ServiceContracts;

public interface IApprovalService
{
    /// <summary>
    /// Gets the pending approvals that the user has to decide on as manager, newest first, one section at a time.
    /// </summary>
    /// <param name="lazyData">How many approvals were already taken and how many to return now.</param>
    /// <param name="userId">The id of the manager.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>A result containing the requested section of approvals waiting for the user.</returns>
    Task<Result<IReadOnlyList<ToApproveDTO>>> GetNeedsApprovalAsync(LazyDTO lazyData, Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the approvals that the user requested, newest first, one section at a time.
    /// </summary>
    /// <param name="lazyData">How many approvals were already taken and how many to return now.</param>
    /// <param name="userId">The id of the user who requested the approvals.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>A result containing the requested section of the user's approvals.</returns>
    Task<Result<IReadOnlyList<RequestedApprovalDTO>>> GetRequested(LazyDTO lazyData, Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Changes the status of an approval. Only the manager of the approval may do this.
    /// </summary>
    /// <param name="approvalId">The id of the approval to update.</param>
    /// <param name="newStatus">The status to apply.</param>
    /// <param name="currentUserId">The id of the user making the change.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>A result containing the updated approval, or a failure when it doesn't exist or the user isn't its manager.</returns>
    Task<Result<RequestedApprovalDTO>> UpdateStatus(Guid approvalId, ApprovalStatusEnum newStatus, Guid currentUserId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates a pending approval and records the matching activity.
    /// </summary>
    /// <param name="toAddApproval">The type of the approval and the task it is about, if any.</param>
    /// <param name="userId">The id of the user requesting the approval.</param>
    /// <param name="managerUserId">The id of the manager who has to decide on it.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>A result containing the created approval as the manager sees it.</returns>
    Task<Result<ToApproveDTO>> AddAsync(ApprovalAddDTO toAddApproval, Guid userId, Guid managerUserId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates a holiday approval for the user, addressed to his parent in the organization hierarchy.
    /// </summary>
    /// <param name="currUserId">The id of the user requesting the holiday.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>A result containing the created approval, or a failure when the user has no parent in the hierarchy.</returns>
    Task<Result<ToApproveDTO>> RequestHoliday(Guid currUserId, CancellationToken cancellationToken = default);
}
