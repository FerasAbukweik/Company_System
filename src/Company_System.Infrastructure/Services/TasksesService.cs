using System.Collections.Immutable;
using System.Net;
using HR_System.Core.common;
using HR_System.Core.Domain.Entities;
using HR_System.Core.DTO.Task;
using HR_System.Core.Enums;
using HR_System.Core.Interfaces.RepositoryContracts;
using HR_System.Core.Interfaces.ServiceContracts.ITaskServices;

namespace HR_System.Infrastructure.Services;

public class TasksesService(ITasksRepository tasksRepository) : ITasksService
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
        
        if(!await tasksRepository.SaveChangesAsync(cancellationToken))
            return Result<TaskDTO>.Failure("Failed to save task");

        return Result<TaskDTO>.Success(toAddTask.ToDTO());
    }

    public async Task<IReadOnlyList<TaskDTO>> GetUserTasksAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var usersTasks = await tasksRepository.GetUserTasksAsync(userId, cancellationToken);

        return usersTasks.Select(t => t.ToDTO()).ToImmutableList();
    }

    public async Task<Result<TaskDTO>> UpdateStatusAsync(Guid currentUserId, Guid taskId, TaskStatusEnum newStatus, CancellationToken cancellationToken = default)
    {
        var updated = await tasksRepository.UpdateStatusAsync(taskId, newStatus, cancellationToken);
        if (updated is null)
            return Result<TaskDTO>.Failure("Failed to update task status or task want found");
        
        if(updated.UserId != currentUserId)
            return  Result<TaskDTO>.Failure("Unauthorized", HttpStatusCode.Unauthorized);
        
        if(!await tasksRepository.SaveChangesAsync(cancellationToken))
            return Result<TaskDTO>.Failure("Failed to save task");
        
        return Result<TaskDTO>.Success(updated.ToDTO(), HttpStatusCode.NoContent);
    }
}