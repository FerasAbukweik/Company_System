using HR_System.Core.Domain.Entities;
using HR_System.Core.DTO.LazyLoading;
using HR_System.Core.Enums;
using HR_System.Core.Interfaces.RepositoryContracts;
using Microsoft.EntityFrameworkCore;

namespace HR_System.Infrastructure.Repositories;

public class TasksRepository(ApplicationDbContext dbContext) : ITasksRepository
{
    public void Add(AppTask task, CancellationToken cancellationToken = default)
    {
        dbContext.Tasks.Add(task);
    }

    public async Task<IReadOnlyList<AppTask>> LazyGetUserTasksAsync(Guid userId, LazyDTO lazyData, CancellationToken cancellationToken = default)
    {
        return await dbContext.Tasks.Where(t => t.UserId == userId)
            .Skip(lazyData.Taken)
            .Take(lazyData.SectionSize)
            .ToListAsync(cancellationToken);
    }

    public async Task<AppTask?> UpdateStatusAsync(Guid taskId, TaskStatusEnum newStatus, CancellationToken cancellationToken = default)
    {
        var toEdit = await dbContext.Tasks.SingleOrDefaultAsync(t => t.Id == taskId, cancellationToken);
        if (toEdit == null) return null!;
        
        toEdit.Status = newStatus;

        return toEdit;
    }

    public async Task<AppTask?> GetTaskAsync(Guid taskId, CancellationToken cancellationToken = default)
    {
        var result = await dbContext.Tasks.FindAsync(taskId, cancellationToken);

        return result;
    }

    public async Task<bool> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return (await dbContext.SaveChangesAsync(cancellationToken)) > 0;
    }
}