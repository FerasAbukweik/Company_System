using System.Net;
using HR_System.Core.Abstraction.RepositoryContracts;
using HR_System.Core.Abstraction.ServiceContracts;
using HR_System.Core.DTO.Activity;
using HR_System.Core.DTO.Approval;
using HR_System.Core.DTO.Task;
using HR_System.Core.Enums;
using HR_System.Core.Result;

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
                TaskId = taskId,
            },
                currentUserId,
                updatedResult.Value!.ManagerId,
                cancellationToken);

            if (!addApprovalResult.IsSuccess)
                return addApprovalResult.MapFailure<TaskDTO>();
        }


        return updatedResult;
    }
    public async Task<Result<RequestedApprovalDTO>> UpdateApprovalStatus(Guid approvalId, ApprovalStatusEnum newStatus, Guid currentUserId, CancellationToken cancellationToken = default)
    {
        if (!Enum.IsDefined(newStatus))
            return Result<RequestedApprovalDTO>.Failure("Invalid approval status", HttpStatusCode.BadRequest);

        // get the approval with the new status applied (not saved yet), then check it before saving
        var updated = await approvalRepository.UpdateStatusAsync(approvalId, newStatus, cancellationToken);
        if (updated is null)
        {
            return Result<RequestedApprovalDTO>.Failure("Failed Updating Approval or Approval Doesnt exist");
        }

        // only the manager may change the status, checked before anything is saved
        if (updated.ManagerId != currentUserId)
        {
            return Result<RequestedApprovalDTO>.Failure("Unauthorized", HttpStatusCode.Forbidden);
        }

        // everything is ok, save
        if (!await approvalRepository.SaveChangesAsync(cancellationToken))
        {
            return Result<RequestedApprovalDTO>.Failure("Failed Updating Approval or Approval Doesnt exist");
        }

        // if approval is for task and approval was rejected set task to rejected
        if (updated.Type == ApprovalTypeEnum.Task && updated.TaskId != null && newStatus == ApprovalStatusEnum.Rejected)
        {
            // saves the task change itself
            var updateTaskResult = await tasksService.UpdateStatusAsync(updated.TaskId.Value, TaskStatusEnum.Rejected, currentUserId, cancellationToken);
            if (!updateTaskResult.IsSuccess) return updateTaskResult.MapFailure<RequestedApprovalDTO>();
        }

        // add activity ------
        // activity type, used later to create activity
        var activityType = newStatus switch
        {
            ApprovalStatusEnum.Approved => ActivityTypeEnum.ApprovalApproved,
            ApprovalStatusEnum.Rejected => ActivityTypeEnum.ApprovalRejected,
            _ => ActivityTypeEnum.MissingType
        };
        // add activity
        // also saves changes
        var addActivityResult = await activitiesService.AddAsync(new ActivityAddDto()
        {
            Type = activityType,
            ApprovalId = updated.Id,
        }, currentUserId, cancellationToken);

        if (!addActivityResult.IsSuccess)
            return addActivityResult.MapFailure<RequestedApprovalDTO>();


        return Result<RequestedApprovalDTO>.Success(updated.ToRequestedApprovalDTO(), HttpStatusCode.NoContent);
    }
}