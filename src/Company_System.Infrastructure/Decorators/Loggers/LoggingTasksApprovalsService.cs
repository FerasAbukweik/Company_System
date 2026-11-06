using HR_System.Core.Abstraction.ServiceContracts;
using HR_System.Core.DTO.Approval;
using HR_System.Core.DTO.Task;
using HR_System.Core.Enums;
using HR_System.Core.Result;
using HR_System.Infrastructure.Services;
using Microsoft.Extensions.Logging;

namespace HR_System.Infrastructure.Decorators.Loggers;

public class LoggingTasksApprovalsService(
    ITasksApprovalsService inner,
    ILogger<LoggingTasksApprovalsService> logger) : ITasksApprovalsService
{
    private const string ServiceName = nameof(TasksApprovalsService);

    public async Task<Result<TaskDTO>> UpdateTaskStatusAsync(Guid currentUserId, Guid taskId, TaskStatusEnum newStatus, CancellationToken cancellationToken = default)
    {
        var result = await inner.UpdateTaskStatusAsync(currentUserId, taskId, newStatus, cancellationToken);
        logger.LogResult(result, ServiceName, nameof(UpdateTaskStatusAsync),
            $"TaskId: {taskId}, NewStatus: {newStatus}, CurrentUserId: {currentUserId}");
        return result;
    }

    public async Task<Result<RequestedApprovalDTO>> UpdateApprovalStatus(Guid approvalId, ApprovalStatusEnum newStatus, Guid currentUserId, CancellationToken cancellationToken = default)
    {
        var result = await inner.UpdateApprovalStatus(approvalId, newStatus, currentUserId, cancellationToken);
        logger.LogResult(result, ServiceName, nameof(UpdateApprovalStatus),
            $"ApprovalId: {approvalId}, NewStatus: {newStatus}, CurrentUserId: {currentUserId}");
        return result;
    }
}
