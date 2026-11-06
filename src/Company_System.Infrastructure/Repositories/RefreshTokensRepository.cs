using System.Linq.Expressions;
using HR_System.Core.Abstraction.RepositoryContracts;
using HR_System.Core.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace HR_System.Infrastructure.Repositories;

public class RefreshTokensRepository(
    ApplicationDbContext dbContext
    ) : IRefreshTokensRepository
{
    // read only, so no tracking
    public async Task<RefreshToken?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await dbContext.RefreshTokens
            .AsNoTracking()
            .SingleOrDefaultAsync(e => e.Id == id, cancellationToken);
    }

    // read only, so no tracking
    public async Task<RefreshToken?> GetByIdAsync(
        Guid id,
        Expression<Func<RefreshToken, object?>>[] include,
        CancellationToken cancellationToken = default)
    {
        IQueryable<RefreshToken> query = dbContext.RefreshTokens.AsNoTracking();

        foreach (var inc in include)
        {
            query = query.Include(inc);
        }

        return await query.SingleOrDefaultAsync(e => e.Id == id, cancellationToken);
    }

    public void AddAsync(RefreshToken refreshToken, CancellationToken cancellationToken = default)
    {
        dbContext.RefreshTokens.Add(refreshToken);
    }

    // read only, so no tracking
    public async Task<RefreshToken?> GetByTokenAsync(string refreshTokenString, CancellationToken cancellationToken = default)
    {
        return await dbContext.RefreshTokens
            .AsNoTracking()
            .SingleOrDefaultAsync(rt => rt.Token == refreshTokenString, cancellationToken);
    }

    public async Task<bool> RemoveAsync(RefreshToken refreshToken, CancellationToken cancellationToken = default)
    {
        // a token is single use: the DELETE affects 1 row for the first caller and 0 for any concurrent second one
        var removed = await dbContext.RefreshTokens
            .Where(rt => rt.Id == refreshToken.Id)
            .ExecuteDeleteAsync(cancellationToken);

        return removed > 0;
    }

    public async Task<IReadOnlyList<RefreshToken>> RemoveExpiredRefreshTokensAsync(CancellationToken cancellationToken = default)
    {
        // one cut-off for both queries, so what is returned is exactly what is deleted
        var now = DateTime.UtcNow;

        var expired = await dbContext.RefreshTokens
            .AsNoTracking()
            .Where(rt => rt.Expires <= now)
            .ToListAsync(cancellationToken);

        if (expired.Count == 0) return expired;

        // ExecuteDeleteAsync is faster than RemoveRange and hits the DB immediately
        await dbContext.RefreshTokens
            .Where(rt => rt.Expires <= now)
            .ExecuteDeleteAsync(cancellationToken);

        return expired;
    }

    public async Task<bool> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return await dbContext.SaveChangesAsync(cancellationToken) > 0;
    }
}
