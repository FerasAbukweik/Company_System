using System.Collections.Immutable;
using System.Net;
using HR_System.Core.common;
using HR_System.Core.Domain.Entities;
using HR_System.Core.DTO.Approval;
using HR_System.Core.Enums;
using HR_System.Core.Interfaces.RepositoryContracts;
using HR_System.Core.Interfaces.ServiceContracts.IApprovalService;

namespace HR_System.Infrastructure.Services;

public class ApprovalService(IApprovalRepository approvalRepository) : IApprovalService
{
    public async Task<Result<IReadOnlyList<ApprovalDTO>>> GetManagerToApproveAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var result = await approvalRepository.GetManagerToApprove(userId, cancellationToken);

        return Result<IReadOnlyList<ApprovalDTO>>.Success(result.Select(r => r.ToDTO()).ToImmutableList());
    }

    public async Task<Result<ApprovalDTO>> AddAsync(ApprovalAddDTO toAddApproval, Guid userId, CancellationToken cancellationToken = default)
    {
        var toAdd = new Approval()
        {
            ManagerId = Guid.NewGuid(), // TODO implement this later
            Type = toAddApproval.Type,
            TaskId = toAddApproval.TaskId,
            UserRequestingId = userId,
        };
        
        // add to DB
        approvalRepository.Add(toAdd);
        
        // save changes
        if(!await approvalRepository.SaveChangesAsync(cancellationToken))
            return Result<ApprovalDTO>.Failure("Failed saving Data to DB");

        return Result<ApprovalDTO>.Success(toAdd.ToDTO());
    }

    public async Task<Result<ApprovalDTO>> UpdateStatus(Guid approvalId, ApprovalStatusEnum newStatus,Guid currentUserId, CancellationToken cancellationToken = default)
    {
        var updated = await approvalRepository.UpdateStatus(approvalId, newStatus, cancellationToken);
        if(updated is null)
            return Result<ApprovalDTO>.Failure("Failed Updating Approval or Approval Doesnt exist");

        if (updated.ManagerId != currentUserId)
            return Result<ApprovalDTO>.Failure("Unauthorized", HttpStatusCode.Unauthorized);
        
        if(!await approvalRepository.SaveChangesAsync(cancellationToken))
            return Result<ApprovalDTO>.Failure("Failed saving Data to DB");
        
        return Result<ApprovalDTO>.Success(updated.ToDTO(), HttpStatusCode.NoContent);
    }
}