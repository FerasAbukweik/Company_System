using HR_System.Core.Abstraction.ServiceContracts;
using HR_System.Core.DTO.LazyLoading;
using HR_System.Core.DTO.Task;
using HR_System.Core.Enums;
using HR_System.Core.Result;
using HR_System.Infrastructure.Services;
using Microsoft.Extensions.Logging;

namespace HR_System.Infrastructure.Decorators.Loggers;

public class LoggingTasksService(
    ITasksService inner,
    ILogger<LoggingTasksService> logger) : ITasksService
{
    private const string ServiceName = nameof(TasksService);

    public async Task<Result<TaskDTO>> AddAsync(TaskAddDTO taskToAddData, Guid currUserId, CancellationToken cancellationToken = default)
    {
        var result = await inner.AddAsync(taskToAddData, currUserId, cancellationToken);
        logger.LogResult(result, ServiceName, nameof(AddAsync),
            $"CurrentUserId: {currUserId}, AssigneeId: {taskToAddData.UserId}, TaskId: {result.Value?.Id}");
        return result;
    }

    public async Task<Result<TaskDTO>> UpdateStatusAsync(Guid taskId, TaskStatusEnum newStatus, Guid currentUserId, CancellationToken cancellationToken = default)
    {
        var result = await inner.UpdateStatusAsync(taskId, newStatus, currentUserId, cancellationToken);
        logger.LogResult(result, ServiceName, nameof(UpdateStatusAsync),
            $"TaskId: {taskId}, NewStatus: {newStatus}, CurrentUserId: {currentUserId}");
        return result;
    }

    public async Task<Result<IReadOnlyList<TaskDTO>>> LazyGetUserTasksAsync(Guid userId, LazyDTO lazyData, CancellationToken cancellationToken = default)
    {
        var result = await inner.LazyGetUserTasksAsync(userId, lazyData, cancellationToken);
        logger.LogResult(result, ServiceName, nameof(LazyGetUserTasksAsync), $"UserId: {userId}", logSuccess: false);
        return result;
    }
}
