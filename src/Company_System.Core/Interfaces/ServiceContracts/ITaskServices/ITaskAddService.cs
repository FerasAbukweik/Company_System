using HR_System.Core.common;
using HR_System.Core.DTO.Task;

namespace HR_System.Core.Interfaces.ServiceContracts.ITaskServices;

public interface ITaskAddService
{
    Task<Result<TaskDTO>> AddAsync(TaskAddDTO toTaskAddData, Guid currUserId, CancellationToken cancellationToken = default);
}