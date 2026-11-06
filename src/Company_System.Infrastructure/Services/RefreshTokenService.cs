using System.Net;
using System.Security.Cryptography;
using HR_System.Core.Abstraction.RepositoryContracts;
using HR_System.Core.Abstraction.ServiceContracts;
using HR_System.Core.Domain.Entities;
using HR_System.Core.Options;
using HR_System.Core.Result;
// Ensure this matches or create IRefreshTokenService
using Microsoft.Extensions.Options;

namespace HR_System.Infrastructure.Services;

public class RefreshTokenService(
    IRefreshTokensRepository refreshTokensRepository,
    IOptions<JWTOptions> jwtOptions) : IRefreshTokenService
{
    public async Task<Result<string>> GenerateRefreshTokenAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        // 1. Generate cryptographically strong random token string
        byte[] bytes = new byte[32];
        using (var rng = RandomNumberGenerator.Create())
        {
            rng.GetBytes(bytes);
        }
        string refreshToken = Convert.ToBase64String(bytes);

        // 2. Prepare database entry
        var lifetimeMinutes = jwtOptions.Value.RefreshTokenLifeTime;
        var toAddRefreshToken = new RefreshToken
        {
            Token = refreshToken,
            UserId = userId,
            Expires = DateTime.UtcNow.AddMinutes(lifetimeMinutes),
        };

        // 3. Save to repository
        refreshTokensRepository.AddAsync(toAddRefreshToken, cancellationToken);
        if (!(await refreshTokensRepository.SaveChangesAsync(cancellationToken)))
        {
            return Result<string>.Failure("Failed saving changes to DB");
        }


        return Result<string>.Success(refreshToken);
    }

    public async Task<Result<RefreshToken>> ConsumeRefreshTokenAsync(string tokenString, CancellationToken cancellationToken = default)
    {
        var removedToken = await refreshTokensRepository.GetByTokenAsync(tokenString, cancellationToken);
        
        if (removedToken == null)
        {

            return Result<RefreshToken>.Failure("Refresh token expired or not found", HttpStatusCode.BadRequest);
        }

        // Remove anyway (one-time use strategy to prevent token reuse replay attacks).
        // false means a concurrent request already consumed this token
        if (!await refreshTokensRepository.RemoveAsync(removedToken, cancellationToken))
        {
            return Result<RefreshToken>.Failure("Refresh token expired or not found", HttpStatusCode.BadRequest);
        }


        // Check if the removed token was already expired or resolved
        if (removedToken.IsResolved) 
        {

            return Result<RefreshToken>.Failure("Expired refresh token", HttpStatusCode.BadRequest);
        }

        return Result<RefreshToken>.Success(removedToken);
    }
}