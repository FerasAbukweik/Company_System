using HR_System.Core.Domain.Entities;
using HR_System.Core.Enums;
using HR_System.Core.Interfaces.RepositoryContracts;
using Microsoft.EntityFrameworkCore;

namespace HR_System.Infrastructure.Repositories;

public class AppTaskRepository(ApplicationDbContext dbContext) : IAppTaskRepository
{
    public void Set(AppTask task, CancellationToken cancellationToken = default)
    {
        dbContext.Tasks.Add(task);
    }

    public async Task<IReadOnlyList<AppTask>> GetUserTasksAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        return await dbContext.Tasks.Where(t => t.UserId == userId).ToListAsync(cancellationToken);
    }

    public async Task<AppTask?> UpdateStatusAsync(Guid taskId, TaskStatusEnum newStatus, CancellationToken cancellationToken = default)
    {
        var toEdit = await dbContext.Tasks.SingleOrDefaultAsync(t => t.Id == taskId, cancellationToken);
        if (toEdit == null) return null!;
        
        toEdit.Status = newStatus;

        return toEdit;
    }

    public async Task<bool> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return (await dbContext.SaveChangesAsync(cancellationToken)) > 0;
    }
}