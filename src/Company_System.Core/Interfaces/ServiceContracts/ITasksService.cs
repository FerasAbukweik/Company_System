using HR_System.Core.common;
using HR_System.Core.DTO.LazyLoading;
using HR_System.Core.DTO.Task;
using HR_System.Core.Enums;

namespace HR_System.Core.Interfaces.ServiceContracts;

public interface ITasksService
{
    Task<Result<TaskDTO>> AddAsync(TaskAddDTO toTaskAddData, Guid currUserId, CancellationToken cancellationToken = default);
    Task<Result<IReadOnlyList<TaskDTO>>> LazyGetUserTasksAsync(Guid userId, LazyDTO lazyData, CancellationToken cancellationToken = default);
    Task<Result<TaskDTO>> UpdateStatusAsync(Guid currentUserId, Guid taskId, TaskStatusEnum newStatus, CancellationToken cancellationToken = default);
    
}