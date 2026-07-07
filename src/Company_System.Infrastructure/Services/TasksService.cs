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

namespace HR_System.Infrastructure.Services;

public class TasksService(ITasksRepository tasksRepository,
    IActivitiesService activitiesService,
    IClaimsService claimsService) : ITasksService
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
        tasksRepository.Add(toAddTask, cancellationToken);
        
        // add activity
        var addActitityResult = await activitiesService.AddAsync(new ActivityAddDTO()
        {
            Type = ActivityTypeEnum.TaskAdded,
            Title = ActivityTextGenerator.GetTaskTitle(toAddTask),
            Description = ActivityTextGenerator.GetTaskDescription(toAddTask, claimsService.GetUserName()),
        }, currUserId, cancellationToken);
        
        if(!addActitityResult.IsSuccess)
            return addActitityResult.MapFailure<TaskDTO>();
        
        if(!await tasksRepository.SaveChangesAsync(cancellationToken))
            return Result<TaskDTO>.Failure("Failed to save task");

        return Result<TaskDTO>.Success(toAddTask.ToDTO());
    }

    public async Task<Result<TaskDTO>> UpdateStatusAsync(Guid taskId, TaskStatusEnum newStatus,Guid currentUserId, CancellationToken cancellationToken = default)
    {
        var updated = await tasksRepository.UpdateStatusAsync(taskId, newStatus, cancellationToken);
        if(updated is null)
            return Result<TaskDTO>.Failure("Failed to update task or task doesnt exist");
        
        if(!(updated.UserId == currentUserId || updated.ManagerId == currentUserId))
            return  Result<TaskDTO>.Failure("Unauthorized", HttpStatusCode.Unauthorized);
        
        if(!await tasksRepository.SaveChangesAsync(cancellationToken))
            return Result<TaskDTO>.Failure("Failed to save updated task to DB");

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