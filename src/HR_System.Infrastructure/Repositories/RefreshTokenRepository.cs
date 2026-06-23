using HR_System.Core.Domain.Entities;
using HR_System.Core.Domain.RepositoryContracts;
using HR_System.Core.Helpers;
using Microsoft.EntityFrameworkCore;

namespace HR_System.Infrastructure.Repositories;

public class RefreshTokenRepository : IRefreshTokenRepository
{
    private readonly ApplicationDbContext _dbContext;
    
    public RefreshTokenRepository(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }
    
    public async Task<Result<RefreshToken>> AddAsync(RefreshToken refreshToken , CancellationToken cancellationToken = default)
    {
        _dbContext.RefreshTokens.Add(refreshToken);
        await _dbContext.SaveChangesAsync(cancellationToken);
        
        return Result<RefreshToken>.Success(refreshToken);
    }

    public async Task<Result<RefreshToken[]>> RemoveExpiredRefreshTokensAsync(CancellationToken cancellationToken = default)
    {
        // get refreshTokens before removing them so we can return them
        var toDel = await _dbContext.RefreshTokens.Where(rt => rt.Expires <= DateTime.UtcNow).ToArrayAsync(cancellationToken);
        
        _dbContext.RemoveRange(toDel);
        await _dbContext.SaveChangesAsync(cancellationToken);
        
        return Result<RefreshToken[]>.Success(toDel);
    }
}