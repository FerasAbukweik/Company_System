using System.Linq.Expressions;
using HR_System.Core.Abstraction.RepositoryContracts;
using HR_System.Core.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace HR_System.Infrastructure.Repositories;

public class ImagesRepository(
    ApplicationDbContext dbContext
    ) : IImagesRepository
{
    // read only, so no tracking
    public async Task<Image?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await dbContext.Images
            .AsNoTracking()
            .SingleOrDefaultAsync(e => e.Id == id, cancellationToken);
    }

    // read only, so no tracking
    public async Task<Image?> GetByIdAsync(
        Guid id,
        Expression<Func<Image, object?>>[] include,
        CancellationToken cancellationToken = default)
    {
        IQueryable<Image> query = dbContext.Images.AsNoTracking();

        foreach (var inc in include)
        {
            query = query.Include(inc);
        }

        return await query.SingleOrDefaultAsync(e => e.Id == id, cancellationToken);
    }

    public async Task<IReadOnlyList<Image>> GetOrphanedImagesAsync(
        DateTime createdBefore,
        CancellationToken cancellationToken = default)
    {
        return await dbContext.Images
            .AsNoTracking()
            .Where(i => i.UserId == null && i.CreatedAt <= createdBefore)
            .OrderBy(i => i.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<int> RemoveOrphanedImagesAsync(IReadOnlyCollection<Guid> imageIds, CancellationToken cancellationToken = default)
    {
        // UserId == null is re-checked in the delete itself, so an image that got an owner in the meantime is kept
        return await dbContext.Images
            .Where(i => imageIds.Contains(i.Id) && i.UserId == null)
            .ExecuteDeleteAsync(cancellationToken);
    }
}
