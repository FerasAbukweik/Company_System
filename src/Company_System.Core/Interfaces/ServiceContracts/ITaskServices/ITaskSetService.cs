using HR_System.Core.common;
using HR_System.Core.DTO.Task;

namespace HR_System.Core.Interfaces.ServiceContracts.ITaskServices;

public interface ITaskSetService
{
    Task<Result<TaskDTO>> SetAsync(AddTaskDTO toAddData, Guid currUserId, CancellationToken cancellationToken = default);
}