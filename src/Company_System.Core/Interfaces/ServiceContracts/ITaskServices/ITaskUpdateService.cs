using HR_System.Core.common;
using HR_System.Core.DTO.Task;
using HR_System.Core.Enums;

namespace HR_System.Core.Interfaces.ServiceContracts.ITaskServices;

public interface ITaskUpdateService
{
    Task<Result<TaskDTO>> UpdateStatusAsync(Guid currentUserId, Guid taskId, TaskStatusEnum newStatus, CancellationToken cancellationToken = default);
}