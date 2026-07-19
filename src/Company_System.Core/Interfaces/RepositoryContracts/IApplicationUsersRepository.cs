using System.Linq.Expressions;
using HR_System.Core.Domain.Identity;

namespace HR_System.Core.Interfaces.RepositoryContracts;

public interface IApplicationUsersRepository
{
    Task<IReadOnlyList<ApplicationUser>> FilterAsync(Expression<Func<ApplicationUser, bool>> checks ,
        Expression<Func<ApplicationUser, Object?>>[]? includes = null,
        CancellationToken cancellationToken = default);
    Task<bool> SaveChangesAsync(CancellationToken cancellationToken = default);
}