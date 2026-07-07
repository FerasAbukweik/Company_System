using System.Net;
using HR_System.Core.common;
using HR_System.Core.DTO.Activity;
using HR_System.Core.DTO.Approval;
using HR_System.Core.DTO.Task;
using HR_System.Core.Enums;
using HR_System.Core.helpers;
using HR_System.Core.Interfaces.RepositoryContracts;
using HR_System.Core.Interfaces.ServiceContracts;

namespace HR_System.Infrastructure.Services;

public class TasksApprovalsService(
    ITasksService tasksService,
    IApprovalService approvalService, 
    IActivitiesService activitiesService,
    IApprovalRepository approvalRepository) : ITasksApprovalsService
{
    public async Task<Result<TaskDTO>> UpdateTaskStatusAsync(Guid currentUserId, Guid taskId, TaskStatusEnum newStatus, CancellationToken cancellationToken = default)
    {
        var updatedResult = await tasksService.UpdateStatusAsync(taskId, newStatus, currentUserId, cancellationToken);
        if (!updatedResult.IsSuccess) return updatedResult;

        // if new status is completed add approval
        if (newStatus == TaskStatusEnum.Completed)
        {
            var addApprovalResult = await approvalService.AddAsync(new ApprovalAddDTO()
                {
                    Type = ApprovalTypeEnum.Task,
                    TaskId = taskId
                },
                currentUserId, cancellationToken);

            if (!addApprovalResult.IsSuccess)
                return addApprovalResult.MapFailure<TaskDTO>();
        }

        return updatedResult;
    }
    public async Task<Result<RequestedApprovalDTO>> UpdateApprovalStatus(Guid approvalId, ApprovalStatusEnum newStatus,Guid currentUserId, CancellationToken cancellationToken = default)
    {
        var updated = await approvalRepository.UpdateStatus(approvalId, newStatus, cancellationToken);
        if(updated is null)
            return Result<RequestedApprovalDTO>.Failure("Failed Updating Approval or Approval Doesnt exist");

        if (updated.ManagerId != currentUserId)
            return Result<RequestedApprovalDTO>.Failure("Unauthorized", HttpStatusCode.Unauthorized);

        // if approval is for task and approval was rejected set task to rejected
        if (updated.Type == ApprovalTypeEnum.Task && updated.TaskId != null && newStatus == ApprovalStatusEnum.Rejected)
        {
            // also saves changes
            var updateTaskResult = await tasksService.UpdateStatusAsync(updated.TaskId.Value, TaskStatusEnum.Rejected, currentUserId, cancellationToken);
            if (!updateTaskResult.IsSuccess) return updateTaskResult.MapFailure<RequestedApprovalDTO>();
        }

        
        // add activity ------
        
        var updatedWithInclude = await approvalRepository.FilterAsync(a => a.Id == updated.Id,
            [a => a.Task!, a => a.UserRequesting!, a => a.Manager!],
            cancellationToken
        );
        
        if(!updatedWithInclude.Any())
            return Result<RequestedApprovalDTO>.Failure("no changes happened to DB");

        // activity type, used later to create activity
        var activityType = newStatus switch
        {
            ApprovalStatusEnum.Approved => ActivityTypeEnum.ApprovalApproved,
            ApprovalStatusEnum.Rejected => ActivityTypeEnum.ApprovalRejected,
            _ => ActivityTypeEnum.MissingType
        };

        // change status to the updated value
        updatedWithInclude[0].Status = newStatus;
        
        // add activity
        // also saves changes
        var addActivityResult = await activitiesService.AddAsync(new ActivityAddDTO()
        {
            Type = activityType,
            Title = ActivityTextGenerator.GetApprovalTitle(updatedWithInclude[0]),
            Description = ActivityTextGenerator.GetApprovalDescription(updatedWithInclude[0]),
        }, currentUserId, cancellationToken);
        
        if(!addActivityResult.IsSuccess)
            return addActivityResult.MapFailure<RequestedApprovalDTO>();
        

        return Result<RequestedApprovalDTO>.Success(updatedWithInclude[0].ToRequestedApprovalDTO(), HttpStatusCode.NoContent);
    }
}