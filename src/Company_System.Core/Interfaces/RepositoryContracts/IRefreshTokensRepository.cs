using HR_System.Core.Domain.Entities;

namespace HR_System.Core.Interfaces.RepositoryContracts;

public interface IRefreshTokensRepository
{
    void AddAsync(RefreshToken refreshToken , CancellationToken cancellationToken = default);
    Task<IReadOnlyList<RefreshToken>> RemoveExpiredRefreshTokensAsync(CancellationToken cancellationToken = default);
    Task<RefreshToken?> FindRefreshTokenByRefreshTokenStringAsync(string refreshTokenString, CancellationToken cancellationToken = default);
    RefreshToken? RemoveRefreshTokenByRefreshTokenString(string refreshTokenString);
    Task<bool> SaveChangesAsync(CancellationToken cancellationToken = default);
}