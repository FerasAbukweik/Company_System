using System.Linq.Expressions;
using HR_System.Core.Domain.Identity;
using Microsoft.EntityFrameworkCore.Storage;

namespace HR_System.Core.Interfaces.RepositoryContracts;

public interface IApplicationUsersRepository
{
    Task<IReadOnlyList<ApplicationUser>> FilterAsync(Expression<Func<ApplicationUser, bool>> checks ,
        Expression<Func<ApplicationUser, Object?>>[]? includes = null,
        CancellationToken cancellationToken = default);
    Task<IDbContextTransaction> BeginTransactionAsync(CancellationToken cancellationToken = default);
    IExecutionStrategy GenerateStrategy();
    Task<bool> SaveChangesAsync(CancellationToken cancellationToken = default);
}