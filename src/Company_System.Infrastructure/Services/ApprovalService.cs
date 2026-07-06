using System.Collections.Immutable;
using System.Net;
using HR_System.Core.common;
using HR_System.Core.Domain.Entities;
using HR_System.Core.DTO.Activity;
using HR_System.Core.DTO.Approval;
using HR_System.Core.DTO.LazyLoading;
using HR_System.Core.Enums;
using HR_System.Core.Interfaces.RepositoryContracts;
using HR_System.Core.Interfaces.ServiceContracts;

namespace HR_System.Infrastructure.Services;

public class ApprovalService(IApprovalRepository approvalRepository,
    IActivitiesService activitiesService,
    IOrganizationHierarchyRepository hierarchyRepository) : IApprovalService
{
    public async Task<Result<IReadOnlyList<ToApproveDTO>>> GetNeedsApprovalAsync(LazyDTO lazyData, Guid userId, CancellationToken cancellationToken = default)
    {
        var result = await approvalRepository.LazyGetApprovals( lazyData,
            (a => (a.ManagerId == userId && a.Status == ApprovalStatusEnum.Pending)),
            [(a => a.Task!), (a => a.UserRequesting!)],
            cancellationToken);

        return Result<IReadOnlyList<ToApproveDTO>>.Success(result.Select(a => a.ToToApprovalDTO()).ToImmutableList());
    }

    public async Task<Result<IReadOnlyList<RequestedApproval>>> GetRequested(LazyDTO lazyData, Guid userId, CancellationToken cancellationToken = default)
    {
        var result = await approvalRepository.LazyGetApprovals(lazyData, 
            (a => a.UserRequestingId == userId),
            [(a => a.Task!), (a => a.UserRequesting!)],
            cancellationToken);

        return Result<IReadOnlyList<RequestedApproval>>.Success(result.Select(a => a.ToRequestedApprovalDTO()).ToImmutableList());
    }
    public async Task<Result<ToApproveDTO>> AddAsync(ApprovalAddDTO toAddApproval, Guid userId, CancellationToken cancellationToken = default)
    {
        var userHierarchy = await hierarchyRepository.GetByUserIdAsync(userId, cancellationToken);
        if(userHierarchy is null)
            return Result<ToApproveDTO>.Failure("User not found in organization hierarchy", HttpStatusCode.BadRequest);
        if (userHierarchy.Parent is null)
            return Result<ToApproveDTO>.Failure("User has no manager in hierarchy", HttpStatusCode.BadRequest);
        
        var toAdd = new Approval()
        {
            ManagerId = userHierarchy.Parent.Id,
            Type = toAddApproval.Type,
            TaskId = toAddApproval.TaskId,
            UserRequestingId = userId,
        };
        
        // add to DB
        approvalRepository.Add(toAdd);

        // add activity
        var addActitityResult = await activitiesService.AddAsync(new ActivityAddDTO()
        {
            Type = ActivityTypeEnum.ApprovalPending,
            ApprovalId = toAdd.Id,
        }, userId, cancellationToken);
        
        if(!addActitityResult.IsSuccess)
            return addActitityResult.MapFailure<ToApproveDTO>();
        
        // save changes
        if(!await approvalRepository.SaveChangesAsync(cancellationToken))
            return Result<ToApproveDTO>.Failure("Failed saving Data to DB");

        return Result<ToApproveDTO>.Success(toAdd.ToToApprovalDTO());
    }
    public async Task<Result<ToApproveDTO>> UpdateStatus(Guid approvalId, ApprovalStatusEnum newStatus,Guid currentUserId, CancellationToken cancellationToken = default)
    {
        var updated = await approvalRepository.UpdateStatus(approvalId, newStatus, cancellationToken);
        if(updated is null)
            return Result<ToApproveDTO>.Failure("Failed Updating Approval or Approval Doesnt exist");

        if (updated.ManagerId != currentUserId)
            return Result<ToApproveDTO>.Failure("Unauthorized", HttpStatusCode.Unauthorized);
        
        // activity type for activity
        var activityType = newStatus switch
        {
            ApprovalStatusEnum.Approved => ActivityTypeEnum.ApprovalApproved,
            ApprovalStatusEnum.Rejected => ActivityTypeEnum.ApprovalRejected,
            _ => ActivityTypeEnum.MissingType
        };
        
        // add activity
        var addActivityResult = await activitiesService.AddAsync(new ActivityAddDTO()
        {
            Type = activityType,
            ApprovalId = approvalId,
        }, currentUserId, cancellationToken);
        
        if(!addActivityResult.IsSuccess)
            return addActivityResult.MapFailure<ToApproveDTO>();

        if (!await approvalRepository.SaveChangesAsync(cancellationToken))
            return Result<ToApproveDTO>.Failure("no changes happened to DB");

        return Result<ToApproveDTO>.Success(updated.ToToApprovalDTO(), HttpStatusCode.NoContent);
    }
}