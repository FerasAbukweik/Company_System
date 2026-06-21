using HR_System.Core.Domain.Entities;
using HR_System.Core.Helpers;

namespace HR_System.Core.Domain.RepositoryContract;

public interface IRefreshTokenRepository
{
    Task<Result<RefreshToken>> AddAsync(RefreshToken refreshToken , CancellationToken cancellationToken = default);
    Task<Result> RemoveExpiredRefreshTokensAsync(CancellationToken cancellationToken = default);
}