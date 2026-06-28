using HR_System.Core.common;
using HR_System.Core.DTO.LazyLoading;
using HR_System.Core.DTO.Task;

namespace HR_System.Core.Interfaces.ServiceContracts.ITaskServices;

public interface ITasksGetService
{
    Task<Result<IReadOnlyList<TaskDTO>>> LazyGetUserTasksAsync(Guid userId, LazyDTO lazyData, CancellationToken cancellationToken = default);
}