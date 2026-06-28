using HR_System.Core.Domain.Entities;
using HR_System.Core.Enums;

namespace HR_System.Core.Interfaces.RepositoryContracts;

public interface ITasksRepository
{
    void Add(AppTask task, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<AppTask>> GetUserTasksAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<AppTask?> UpdateStatusAsync(Guid taskId, TaskStatusEnum newStatus, CancellationToken cancellationToken = default);
    Task<bool> SaveChangesAsync(CancellationToken cancellationToken = default);
}