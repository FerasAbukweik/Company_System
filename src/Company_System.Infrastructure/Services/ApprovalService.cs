using System.Collections.Immutable;
using System.Net;
using HR_System.Core.common;
using HR_System.Core.Domain.Entities;
using HR_System.Core.Domain.Identity;
using HR_System.Core.DTO.Activity;
using HR_System.Core.DTO.Approval;
using HR_System.Core.Enums;
using HR_System.Core.Interfaces.RepositoryContracts;
using HR_System.Core.Interfaces.ServiceContracts.IActivitiesService;
using HR_System.Core.Interfaces.ServiceContracts.IApprovalService;
using Microsoft.AspNetCore.Identity;

namespace HR_System.Infrastructure.Services;

public class ApprovalService(IApprovalRepository approvalRepository,
    UserManager<ApplicationUser> userManager,
    ITasksRepository tasksRepository,
    IActivitiesService activitiesService,
    IOrganizationHierarchyRepository hierarchyRepository) : IApprovalService
{
    public async Task<Result<IReadOnlyList<ApprovalDTO>>> GetNeedsApprovalAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var result = await approvalRepository.GetNeedsApprovalAsync(userId, cancellationToken);

        return Result<IReadOnlyList<ApprovalDTO>>.Success(result.Select(r => r.ToDTO()).ToImmutableList());
    }

    public async Task<Result<ApprovalDTO>> AddAsync(ApprovalAddDTO toAddApproval, Guid userId, CancellationToken cancellationToken = default)
    {
        var generateDescriptionResult =
            await GenerateApprovalDescription(toAddApproval.TaskId, userId, toAddApproval.Type, cancellationToken);
        if (!generateDescriptionResult.IsSuccess) return generateDescriptionResult.MapFailure<ApprovalDTO>();
        
        var userHierarchy = await hierarchyRepository.GetByUserIdAsync(userId, cancellationToken);
        if(userHierarchy is null)
            return Result<ApprovalDTO>.Failure("User not found in organization hierarchy", HttpStatusCode.BadRequest);
        if (userHierarchy.Parent is null)
            return Result<ApprovalDTO>.Failure("User has no manager in hierarchy", HttpStatusCode.BadRequest);
        
        var toAdd = new Approval()
        {
            ManagerId = userHierarchy.Parent.Id,
            Type = toAddApproval.Type,
            TaskId = toAddApproval.TaskId,
            UserRequestingId = userId,
            Description = generateDescriptionResult.Value!,
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
            return addActitityResult.MapFailure<ApprovalDTO>();
        
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
        
        // activity type for activity
        var activityType = newStatus switch
        {
            ApprovalStatusEnum.Approved => ActivityTypeEnum.ApprovalApproved,
            ApprovalStatusEnum.Rejected => ActivityTypeEnum.ApprovalRejected,
            _ => ActivityTypeEnum.MissingType
        };
        
        // add activity
        var addActitityResult = await activitiesService.AddAsync(new ActivityAddDTO()
        {
            Type = activityType,
            ApprovalId = approvalId,
        }, currentUserId, cancellationToken);
        
        if(!addActitityResult.IsSuccess)
            return addActitityResult.MapFailure<ApprovalDTO>();
        
        if(!await approvalRepository.SaveChangesAsync(cancellationToken))
            return Result<ApprovalDTO>.Failure("Failed saving Data to DB");
        
        return Result<ApprovalDTO>.Success(updated.ToDTO(), HttpStatusCode.NoContent);
    }
    
    
    
    
    private async Task<Result<string>> GenerateApprovalDescription(Guid? taskId, Guid currUserId, ApprovalTypeEnum approvalType, CancellationToken cancellationToken = default)
    {
        if (approvalType == ApprovalTypeEnum.Holiday)
        {
            var currentRequestUser = await userManager.FindByIdAsync(currUserId.ToString());
            if(currentRequestUser is null)
                return Result<string>.Failure("user not found", HttpStatusCode.Unauthorized);
            
            string result = $"{currentRequestUser.UserName} is requesting holiday";
            return Result<string>.Success(result);
        }

        if (approvalType == ApprovalTypeEnum.Task)
        {
            if(taskId is null)
                return Result<string>.Failure("taskId is required", HttpStatusCode.BadRequest);
            
            var task = await tasksRepository.GetTaskAsync(taskId.Value, cancellationToken);
            if(task is null)
                return  Result<string>.Failure("task not found", HttpStatusCode.NotFound);

            string result = task.Description;
            return Result<string>.Success(result);
        }
        
        return Result<string>.Failure("not handled ApprovalType", HttpStatusCode.NotFound);
    }
}