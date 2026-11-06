using System.Collections.Immutable;
using System.Net;
using HR_System.Core.Abstraction.RepositoryContracts;
using HR_System.Core.Abstraction.ServiceContracts;
using HR_System.Core.Domain.Entities;
using HR_System.Core.DTO.Activity;
using HR_System.Core.DTO.LazyLoading;
using HR_System.Core.DTO.Task;
using HR_System.Core.Enums;
using HR_System.Core.Result;

namespace HR_System.Infrastructure.Services;

public class TasksService(ITasksRepository tasksRepository,
    IActivitiesService activitiesService,
    IOrganizationHierarchyService hierarchyService) : ITasksService
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
        if (!parentIds.IsSuccess) return parentIds.MapFailure<TaskDTO>();
        // check if curr user has permission to add task
        if (!parentIds.Value!.Contains(currUserId))
        {
            return Result<TaskDTO>.Failure("Unauthorized", HttpStatusCode.Forbidden);
        }

        tasksRepository.Add(toAddTask, cancellationToken);
        // add activity
        // also saves changes
        var addActivityResult = await activitiesService.AddAsync(new ActivityAddDto()
        {
            Type = ActivityTypeEnum.TaskAdded,
            TaskId = toAddTask.Id,
        }, currUserId, cancellationToken);

        if (!addActivityResult.IsSuccess)
            return addActivityResult.MapFailure<TaskDTO>();


        return Result<TaskDTO>.Success(toAddTask.ToDTO());
    }

    public async Task<Result<TaskDTO>> UpdateStatusAsync(Guid taskId, TaskStatusEnum newStatus, Guid currentUserId, CancellationToken cancellationToken = default)
    {
        if (!Enum.IsDefined(newStatus))
            return Result<TaskDTO>.Failure("Invalid task status", HttpStatusCode.BadRequest);

        // get the task with the new status applied (not saved yet), then check it before saving
        var updated = await tasksRepository.UpdateStatusAsync(taskId, newStatus, cancellationToken);
        if (updated is null)
        {
            return Result<TaskDTO>.Failure("Failed to update task or task doesnt exist");
        }

        // check if curr user has access to update task
        if (!(updated.UserId == currentUserId || updated.ManagerId == currentUserId))
        {
            return Result<TaskDTO>.Failure("Unauthorized", HttpStatusCode.Forbidden);
        }

        // only manager is allowed to change status to rejected or pending
        if ((newStatus == TaskStatusEnum.Rejected || newStatus == TaskStatusEnum.Pending) &&
            currentUserId != updated.ManagerId)
        {
            return Result<TaskDTO>.Failure("Unauthorized", HttpStatusCode.Forbidden);
        }

        // everything is ok, save
        if (!await tasksRepository.SaveChangesAsync(cancellationToken))
        {
            return Result<TaskDTO>.Failure("Failed to update task or task doesnt exist");
        }


        return Result<TaskDTO>.Success(updated.ToDTO(), HttpStatusCode.NoContent);

    }

    public async Task<Result<IReadOnlyList<TaskDTO>>> LazyGetUserTasksAsync(Guid userId, LazyDTO lazyData, CancellationToken cancellationToken = default)
    {
        if (lazyData.Taken < 0)
            return Result<IReadOnlyList<TaskDTO>>.Failure("Taken cannot be negative", HttpStatusCode.BadRequest);

        // the user's unfinished tasks, newest first, one section at a time
        var usersTasks = await tasksRepository.FilterAsync(
            t => t.UserId == userId && t.Status != TaskStatusEnum.Completed,
            null,
            t => t.CreatedAt,
            true,
            lazyData.Taken,
            lazyData.SectionSize,
            cancellationToken);

        return Result<IReadOnlyList<TaskDTO>>.Success(usersTasks.Select(t => t.ToDTO()).ToImmutableList());
    }
}