using System.Collections.Immutable;
using System.Net;
using HR_System.Core.common;
using HR_System.Core.Domain.Entities;
using HR_System.Core.DTO.Task;
using HR_System.Core.Enums;
using HR_System.Core.Interfaces.RepositoryContracts;
using HR_System.Core.Interfaces.ServiceContracts.ITaskServices;

namespace HR_System.Infrastructure.Services;

public class TasksesService(IAppTaskRepository taskRepository) : ITasksService
{
    public async Task<Result<TaskDTO>> SetAsync(AddTaskDTO toAddData, Guid currUserId, CancellationToken cancellationToken = default)
    {
        var toAddTask = new AppTask()
        {
            ManagerId = currUserId,
            UserId = toAddData.UserId,
            Title = toAddData.Title,
            Description = toAddData.Description,
            Priority = toAddData.Priority,
            Created = DateTime.UtcNow,
            Deadline = toAddData.Deadline,
        };
        taskRepository.Set(toAddTask);
        
        if(!await taskRepository.SaveChangesAsync(cancellationToken))
            return Result<TaskDTO>.Failure("Failed to save task");

        return Result<TaskDTO>.Success(toAddTask.ToDTO());
    }

    public async Task<IReadOnlyList<TaskDTO>> GetUserTasksAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var usersTasks = await taskRepository.GetUserTasksAsync(userId, cancellationToken);

        return usersTasks.Select(t => t.ToDTO()).ToImmutableList();
    }

    public async Task<Result<TaskDTO>> UpdateStatus(Guid currentUserId, Guid taskId, TaskStatusEnum newStatus, CancellationToken cancellationToken = default)
    {
        var updated = await taskRepository.UpdateStatusAsync(taskId, newStatus, cancellationToken);
        if (updated is null)
            return Result<TaskDTO>.Failure("Failed to update task status or task want found");
        
        if(updated.UserId != currentUserId)
            return  Result<TaskDTO>.Failure("Unauthorized", HttpStatusCode.Unauthorized);
        
        if(!await taskRepository.SaveChangesAsync(cancellationToken))
            return Result<TaskDTO>.Failure("Failed to save task");
        
        return Result<TaskDTO>.Success(updated.ToDTO());
    }
}