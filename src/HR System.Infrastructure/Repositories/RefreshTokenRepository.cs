using HR_System.Core.Domain.Entities;
using HR_System.Core.Domain.RepositoryContract;
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

    public async Task<Result> RemoveExpiredRefreshTokensAsync(CancellationToken cancellationToken = default)
    {
        await _dbContext.RefreshTokens.Where(rt => rt.Expires <= DateTime.UtcNow)
            .ExecuteDeleteAsync(cancellationToken);
        
        return Result.Success();
    }
    
}