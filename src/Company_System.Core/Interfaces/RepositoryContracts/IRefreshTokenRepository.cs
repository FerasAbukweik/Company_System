using HR_System.Core.Domain.Entities;

namespace HR_System.Core.Interfaces.RepositoryContracts;

public interface IRefreshTokenRepository
{
    void AddAsync(RefreshToken refreshToken , CancellationToken cancellationToken = default);
    Task<IReadOnlyList<RefreshToken>> RemoveExpiredRefreshTokensAsync(CancellationToken cancellationToken = default);
    Task<RefreshToken?> FindRefreshTokenByRefreshTokenStringAsync(string refreshTokenString, CancellationToken cancellationToken = default);
    Task<bool> SaveChangesAsync(CancellationToken cancellationToken = default);
}