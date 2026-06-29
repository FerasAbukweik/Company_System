using System.Collections.Immutable;
using System.Net;
using HR_System.Core.common;
using HR_System.Core.Domain.Entities;
using HR_System.Core.DTO.Activity;
using HR_System.Core.DTO.LazyLoading;
using HR_System.Core.DTO.Task;
using HR_System.Core.Enums;
using HR_System.Core.Interfaces.RepositoryContracts;
using HR_System.Core.Interfaces.ServiceContracts.IActivitiesService;
using HR_System.Core.Interfaces.ServiceContracts.ITaskServices;

namespace HR_System.Infrastructure.Services;

public class TasksService(ITasksRepository tasksRepository,
    IActivitiesService activitiesService) : ITasksService
{
    public async Task<Result<TaskDTO>> AddAsync(TaskAddDTO toTaskAddData, Guid currUserId, CancellationToken cancellationToken = default)
    {
        var toAddTask = new AppTask()
        {
            ManagerId = currUserId,
            UserId = toTaskAddData.UserId,
            Title = toTaskAddData.Title,
            Description = toTaskAddData.Description,
            Priority = toTaskAddData.Priority,
            Created = DateTime.UtcNow,
            Deadline = toTaskAddData.Deadline,
        };
        tasksRepository.Add(toAddTask);
        
        // add activity
        var addActitityResult = await activitiesService.AddAsync(new ActivityAddDTO()
        {
            Type = ActivityTypeEnum.TaskAdded,
            TaskId = toAddTask.Id,
        }, currUserId, cancellationToken);
        
        if(!addActitityResult.IsSuccess)
            return addActitityResult.MapFailure<TaskDTO>();
        
        if(!await tasksRepository.SaveChangesAsync(cancellationToken))
            return Result<TaskDTO>.Failure("Failed to save task");

        return Result<TaskDTO>.Success(toAddTask.ToDTO());
    }

    public async Task<Result<IReadOnlyList<TaskDTO>>> LazyGetUserTasksAsync(Guid userId, LazyDTO lazyData, CancellationToken cancellationToken = default)
    {
        if (lazyData.Taken < 0)
            return Result<IReadOnlyList<TaskDTO>>.Failure("Taken cannot be negative", HttpStatusCode.BadRequest);
        
        var usersTasks = await tasksRepository.LazyGetUserTasksAsync(userId,lazyData, cancellationToken);

        return Result<IReadOnlyList<TaskDTO>>.Success(usersTasks.Select(t => t.ToDTO()).ToImmutableList());
    }

    public async Task<Result<TaskDTO>> UpdateStatusAsync(Guid currentUserId, Guid taskId, TaskStatusEnum newStatus, CancellationToken cancellationToken = default)
    {
        var updated = await tasksRepository.UpdateStatusAsync(taskId, newStatus, cancellationToken);
        if (updated is null)
            return Result<TaskDTO>.Failure("Failed to update task status or task want found");
        
        if(updated.UserId != currentUserId)
            return  Result<TaskDTO>.Failure("Unauthorized", HttpStatusCode.Unauthorized);
        
        
        // activity type for activity
        var activityType = newStatus switch
        {
            TaskStatusEnum.Completed => ActivityTypeEnum.TaskCompleted,
            TaskStatusEnum.Pending => ActivityTypeEnum.TaskPendingApproval,
            TaskStatusEnum.Rejected => ActivityTypeEnum.TaskRejected,
            _ => ActivityTypeEnum.MissingType
        };
        
        // add activity
        var addActitityResult = await activitiesService.AddAsync(new ActivityAddDTO()
        {
            Type = activityType,
            TaskId = taskId,
        }, currentUserId, cancellationToken);
        
        if(!addActitityResult.IsSuccess)
            return addActitityResult.MapFailure<TaskDTO>();
        
        
        if(!await tasksRepository.SaveChangesAsync(cancellationToken))
            return Result<TaskDTO>.Failure("Failed to save task");
        
        return Result<TaskDTO>.Success(updated.ToDTO(), HttpStatusCode.NoContent);
    }
}