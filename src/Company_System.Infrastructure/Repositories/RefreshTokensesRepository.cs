using HR_System.Core.Domain.Entities;
using HR_System.Core.Interfaces.RepositoryContracts;
using HR_System.Core.Interfaces.ServiceContracts;
using Microsoft.EntityFrameworkCore;

namespace HR_System.Infrastructure.Repositories;

public class RefreshTokensesRepository(ApplicationDbContext dbContext,
    IRedisService cache) : IRefreshTokensRepository
{
    public void AddAsync(RefreshToken refreshToken , CancellationToken cancellationToken = default)
    {
        dbContext.RefreshTokens.Add(refreshToken);
    }

    public async Task<IReadOnlyList<RefreshToken>> RemoveExpiredRefreshTokensAsync(CancellationToken cancellationToken = default)
    {
        // get refreshTokens before removing them so we can return them
        var toDel = await dbContext.RefreshTokens.Where(rt => rt.Expires <= DateTime.UtcNow)
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        // deleting expired tokens from DB
        // using ExecuteDeleteAsync because it's faster than RemoveRange
        await dbContext.RefreshTokens.Where(rt => rt.Expires <= DateTime.UtcNow).ExecuteDeleteAsync(cancellationToken);

        return toDel;
    }

    public async Task<RefreshToken?> FindRefreshTokenByRefreshTokenStringAsync(
        string refreshTokenString,
        CancellationToken cancellationToken = default
        )
    {
        // check if cache has the token
        var cachedToken = await cache.Get<RefreshToken>(refreshTokenString, cancellationToken);
        if (cachedToken != null) return cachedToken;
        
        // get token from Db
        var token = await dbContext.RefreshTokens.SingleOrDefaultAsync(rt => rt.Token == refreshTokenString, cancellationToken);

        // set token to cache
        await cache.Set(refreshTokenString, token, cancellationToken);
        
        return token;
    }

    public RefreshToken? RemoveRefreshTokenByRefreshTokenString(string refreshTokenString)
    {
        var toRemove = dbContext.RefreshTokens.SingleOrDefault(rt => rt.Token == refreshTokenString);
        if (toRemove == null) return null;

        dbContext.RefreshTokens.Remove(toRemove);
        
        return toRemove;
    }

    public async Task<bool> SaveChangesAsync(CancellationToken cancellationToken = default) =>  (await dbContext.SaveChangesAsync(cancellationToken)) > 0;
}