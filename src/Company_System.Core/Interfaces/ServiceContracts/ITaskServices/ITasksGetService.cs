using HR_System.Core.common;
using HR_System.Core.DTO.Task;

namespace HR_System.Core.Interfaces.ServiceContracts.ITaskServices;

public interface ITasksGetService
{
    Task<IReadOnlyList<TaskDTO>> GetUserTasksAsync(Guid userId, CancellationToken cancellationToken = default);
}