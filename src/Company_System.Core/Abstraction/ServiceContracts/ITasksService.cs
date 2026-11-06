using HR_System.Core.DTO.LazyLoading;
using HR_System.Core.DTO.Task;
using HR_System.Core.Enums;
using HR_System.Core.Result;

namespace HR_System.Core.Abstraction.ServiceContracts;

public interface ITasksService
{
    /// <summary>
    /// Creates a task for a user under the current user and records the matching activity.
    /// Only a user above the assignee in the hierarchy may add the task.
    /// </summary>
    /// <param name="taskToAddData">The title, description, priority, deadline and assignee of the task.</param>
    /// <param name="currUserId">The id of the user adding the task (becomes its manager).</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>A result containing the created task, or a failure when the user isn't allowed to assign tasks to the assignee.</returns>
    Task<Result<TaskDTO>> AddAsync(TaskAddDTO taskToAddData, Guid currUserId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Changes the status of a task. Only its assignee or manager may do this,
    /// and only the manager may set it to rejected or pending.
    /// </summary>
    /// <param name="taskId">The id of the task to update.</param>
    /// <param name="newStatus">The status to apply.</param>
    /// <param name="currentUserId">The id of the user making the change.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>A result containing the updated task, or a failure when it doesn't exist or the user isn't allowed to make this change.</returns>
    Task<Result<TaskDTO>> UpdateStatusAsync(Guid taskId, TaskStatusEnum newStatus, Guid currentUserId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the unfinished tasks assigned to a user, newest first, one section at a time.
    /// </summary>
    /// <param name="userId">The id of the user the tasks are assigned to.</param>
    /// <param name="lazyData">How many tasks were already taken and how many to return now.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>A result containing the requested section of tasks.</returns>
    Task<Result<IReadOnlyList<TaskDTO>>> LazyGetUserTasksAsync(Guid userId, LazyDTO lazyData, CancellationToken cancellationToken = default);
}
