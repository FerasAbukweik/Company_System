using System.Linq.Expressions;
using HR_System.Core.Domain.Idnetity;
using HR_System.Core.Helpers;
using Microsoft.EntityFrameworkCore.Storage;

namespace HR_System.Core.Domain.RepositoryContracts;

public interface IApplicationUserRepository
{
    Task<Result<ApplicationUser[]>> FilterAsync(Expression<Func<ApplicationUser, bool>> checks ,
        Expression<Func<ApplicationUser, Object?>>[]? includes = null,
        CancellationToken cancellationToken = default);
    Task<IDbContextTransaction> BeginTransactionAsync(CancellationToken cancellationToken = default);
}