using System.Linq.Expressions;
using System.Text;
using HR_System.Core.Domain.Idnetity;
using HR_System.Core.Interfaces.RepositoryContracts;
using HR_System.Core.Interfaces.ServiceContracts.IRedisService;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace HR_System.Infrastructure.Repositories;

public class ApplicationUsersesRepository(ApplicationDbContext dbContext) : IApplicationUsersRepository
{
    public async Task<IReadOnlyList<ApplicationUser>> FilterAsync(
        Expression<Func<ApplicationUser, bool>> checks,
        Expression<Func<ApplicationUser, object?>>[]? includes = null,
        CancellationToken cancellationToken = default)
    {
        
        // create users query
        var query = dbContext.Users.AsQueryable();

        // including required fields
        if (includes != null && includes.Any())
        {
            foreach (var include in includes)
            {
                query = query.Include(include);
            }
        }
        
        // fetching/returning result
        return await query
            .AsNoTracking()
            .Where(checks).ToListAsync(cancellationToken);
    }

    public async Task<IDbContextTransaction> BeginTransactionAsync(CancellationToken cancellationToken = default)
    {
        return await dbContext.Database.BeginTransactionAsync(cancellationToken);
    }
    
    public async Task<bool> SaveChangesAsync(CancellationToken cancellationToken = default) =>  (await dbContext.SaveChangesAsync(cancellationToken)) > 0;
}