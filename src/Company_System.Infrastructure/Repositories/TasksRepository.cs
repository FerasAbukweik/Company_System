using HR_System.Core.Domain.Entities;
using HR_System.Core.DTO.LazyLoading;
using HR_System.Core.Enums;
using HR_System.Core.Interfaces.RepositoryContracts;
using HR_System.Core.Interfaces.ServiceContracts;
using Microsoft.EntityFrameworkCore;

namespace HR_System.Infrastructure.Repositories;

public class TasksRepository(ApplicationDbContext dbContext,
    IRedisService cache) : ITasksRepository
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
            .AsNoTracking()
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
        var cachedValue = await cache.GetAsync<AppTask>($"Task-Id-{taskId}", cancellationToken);
        if(cachedValue != null) return cachedValue;
        
        var result = await dbContext.Tasks
            .AsNoTracking()
            .SingleOrDefaultAsync(t => t.Id == taskId, cancellationToken);

        if (result != null) await cache.SetAsync($"Task-Id-{taskId}", result, cancellationToken);

        return result;
    }

    public async Task<bool> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return (await dbContext.SaveChangesAsync(cancellationToken)) > 0;
    }
}