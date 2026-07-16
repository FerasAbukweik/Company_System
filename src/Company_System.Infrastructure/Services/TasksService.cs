using System.Collections.Immutable;
using System.Net;
using HR_System.Core.common;
using HR_System.Core.Domain.Entities;
using HR_System.Core.DTO.Activity;
using HR_System.Core.DTO.LazyLoading;
using HR_System.Core.DTO.Task;
using HR_System.Core.Enums;
using HR_System.Core.helpers;
using HR_System.Core.Interfaces.RepositoryContracts;
using HR_System.Core.Interfaces.ServiceContracts;
using Microsoft.Extensions.Logging;

namespace HR_System.Infrastructure.Services;

public class TasksService(ITasksRepository tasksRepository,
    IActivitiesService activitiesService,
    IClaimsService claimsService,
    IOrganizationHierarchyService hierarchyService,
    ILogger<TasksService> logger) : ITasksService
{
    public async Task<Result<TaskDTO>> AddAsync(TaskAddDTO taskToAddData, Guid currUserId, CancellationToken cancellationToken = default)
    {
        var toAddTask = new AppTask()
        {
            ManagerId = currUserId,
            UserId = taskToAddData.UserId,
            Title = taskToAddData.Title,
            Description = taskToAddData.Description,
            Priority = taskToAddData.Priority,
            CreatedAt = DateTime.UtcNow,
            Deadline = taskToAddData.Deadline,
        };
        
        // get parent ids
        var parentIds = await hierarchyService.GetParentUserIds(taskToAddData.UserId, cancellationToken);
        if(!parentIds.IsSuccess) return parentIds.MapFailure<TaskDTO>();
        
        // check if curr user has permission to add task
        if (!parentIds.Value!.Contains(currUserId))
        {
            logger.LogWarning("{serviceName}.{methodName} user with id {currUserId} tried adding task for user id {otherUserId} which he doesnt have access to",
                nameof(TasksService), nameof(AddAsync), currUserId, toAddTask.UserId);
            return Result<TaskDTO>.Failure("Unauthorized", HttpStatusCode.Unauthorized);
        }

        tasksRepository.Add(toAddTask, cancellationToken);
        
        // add activity
        // also saves changes
        var addActivityResult = await activitiesService.AddAsync(new ActivityAddDTO()
        {
            Type = ActivityTypeEnum.TaskAdded,
            Title = ActivityTextGenerator.GetTaskTitle(toAddTask),
            Description = ActivityTextGenerator.GetTaskDescription(toAddTask, claimsService.GetUserName()),
        }, currUserId, cancellationToken);
        
        if(!addActivityResult.IsSuccess)
            return addActivityResult.MapFailure<TaskDTO>();
        
        logger.LogInformation("{serviceName}.{methodName} task with id {taskId} was added by user {currUserId}",
            nameof(TasksService), nameof(AddAsync), toAddTask.Id, currUserId);
        
        return Result<TaskDTO>.Success(toAddTask.ToDTO());
    }

    public async Task<Result<TaskDTO>> UpdateStatusAsync(Guid taskId, TaskStatusEnum newStatus,Guid currentUserId, CancellationToken cancellationToken = default)
    {
        // update status
        var updated = await tasksRepository.UpdateStatusAsync(taskId, newStatus, cancellationToken);
        if (updated is null)
        {
            logger.LogWarning("{serviceName}.{methodName} user with id {currUserId} tried updating task with id {taskId} which doesnt exist",
                nameof(TasksService), nameof(UpdateStatusAsync), currentUserId, taskId);
            return Result<TaskDTO>.Failure("Failed to update task or task doesnt exist");
        }

        // check if curr user has access to update task
        if (!(updated.UserId == currentUserId || updated.ManagerId == currentUserId))
        {
            logger.LogWarning("{serviceName}.{methodName} user with id {currUserId} tried updating task with id {taskId} which he doesnt have access to do",
                nameof(TasksService), nameof(UpdateStatusAsync), currentUserId, taskId);
            return  Result<TaskDTO>.Failure("Unauthorized", HttpStatusCode.Unauthorized);
        }
        
        // only manager is allowed to change status to rejected or pending
        if ((newStatus == TaskStatusEnum.Rejected || newStatus == TaskStatusEnum.Pending) &&
            currentUserId != updated.ManagerId)
        {
            logger.LogWarning("{serviceName}.{methodName} user with id {currUserId} tried updating task with id {taskId} which he doesnt have access to do",
                nameof(TasksService), nameof(UpdateStatusAsync), currentUserId, taskId);
            return  Result<TaskDTO>.Failure("Unauthorized", HttpStatusCode.Unauthorized);
        }

        if (!await tasksRepository.SaveChangesAsync(cancellationToken))
        {
            logger.LogWarning("{serviceName}.{methodName} failed saving changes to DB",
                nameof(TasksService), nameof(UpdateStatusAsync));
            return Result<TaskDTO>.Failure("Failed to save updated task to DB");
        }
        
        logger.LogWarning("{serviceName}.{methodName} task with id {taskId} was updated by user {currUserId}",
            nameof(TasksService), nameof(UpdateStatusAsync), taskId, currentUserId);

        return Result<TaskDTO>.Success(updated.ToDTO(), HttpStatusCode.NoContent);

    }

    public async Task<Result<IReadOnlyList<TaskDTO>>> LazyGetUserTasksAsync(Guid userId, LazyDTO lazyData, CancellationToken cancellationToken = default)
    {
        if (lazyData.Taken < 0)
            return Result<IReadOnlyList<TaskDTO>>.Failure("Taken cannot be negative", HttpStatusCode.BadRequest);
        
        var usersTasks = await tasksRepository.LazyGetUserTasksAsync(userId,lazyData, cancellationToken);

        return Result<IReadOnlyList<TaskDTO>>.Success(usersTasks.Select(t => t.ToDTO()).ToImmutableList());
    }
}