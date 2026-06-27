using System.Linq.Expressions;
using HR_System.Core.Domain.Idnetity;
using Microsoft.EntityFrameworkCore.Storage;

namespace HR_System.Core.Interfaces.RepositoryContracts;

public interface IApplicationUserRepository
{
    Task<IReadOnlyList<ApplicationUser>> FilterAsync(Expression<Func<ApplicationUser, bool>> checks ,
        Expression<Func<ApplicationUser, Object?>>[]? includes = null,
        CancellationToken cancellationToken = default);
    Task<IDbContextTransaction> BeginTransactionAsync(CancellationToken cancellationToken = default);
    Task<bool> SaveChangesAsync(CancellationToken cancellationToken = default);
}