using System.Linq.Expressions;
using HR_System.Core.Domain.Idnetity;
using HR_System.Core.Domain.RepositoryContracts;
using HR_System.Core.Helpers;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace HR_System.Infrastructure.Repositories;

public class ApplicationUserRepository : IApplicationUserRepository
{
    private readonly ApplicationDbContext _dbContext;
    
    public ApplicationUserRepository(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }
    public async Task<Result<ApplicationUser[]>> FilterAsync(Expression<Func<ApplicationUser, bool>> checks,
        Expression<Func<ApplicationUser, object?>>[]? includes = null,
        CancellationToken cancellationToken = default)
    {
        var query = _dbContext.Users.AsQueryable();

        if (includes != null && includes.Any())
        {
            foreach (var include in includes)
            {
                query = query.Include(include);
            }
        }
        
        var result = await query.Where(checks).ToArrayAsync(cancellationToken);

        return Result<ApplicationUser[]>.Success(result);
    }

    public async Task<IDbContextTransaction> BeginTransactionAsync(CancellationToken cancellationToken = default)
    {
        return await _dbContext.Database.BeginTransactionAsync(cancellationToken);
    }
}