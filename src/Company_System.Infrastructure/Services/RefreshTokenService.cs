using System.Net;
using System.Security.Cryptography;
using HR_System.Core.common;
using HR_System.Core.Domain.Entities;
using HR_System.Core.Interfaces.RepositoryContracts;
using HR_System.Core.Interfaces.ServiceContracts; // Ensure this matches or create IRefreshTokenService
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace HR_System.Infrastructure.Services;

public class RefreshTokenService(
    IRefreshTokensRepository refreshTokensRepository,
    IConfiguration configuration,
    ILogger<RefreshTokenService> logger) : IRefreshTokenService
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
        var lifetimeMinutes = configuration.GetValue<int>("Jwt:RefreshTokenLifeTime");
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
            logger.LogError("{ServiceName}.{MethodName} failed saving changes to DB",
                nameof(RefreshTokenService), nameof(GenerateRefreshTokenAsync));
            return Result<string>.Failure("Failed saving changes to DB");
        }

        logger.LogInformation("{ServiceName}.{MethodName} successfully created and saved refresh token in DB for User ID {UserId}.",
            nameof(RefreshTokenService), nameof(GenerateRefreshTokenAsync), userId);

        return Result<string>.Success(refreshToken);
    }

    public async Task<Result<RefreshToken>> ConsumeRefreshTokenAsync(string tokenString, CancellationToken cancellationToken = default)
    {
        // Remove anyway (one-time use strategy to prevent token reuse replay attacks)
        var removedToken = await refreshTokensRepository.RemoveRefreshTokenByRefreshTokenString(tokenString, cancellationToken);
        
        if (removedToken == null)
        {
            logger.LogWarning("{ServiceName}.{MethodName} failed: Refresh token string was either not found in DB or has already been used/removed.",
                nameof(RefreshTokenService), nameof(ConsumeRefreshTokenAsync));

            return Result<RefreshToken>.Failure("Refresh token expired or not found", HttpStatusCode.BadRequest);
        }

        logger.LogInformation("{ServiceName}.{MethodName} successfully removed consumed token string from DB for User ID {UserId}.",
            nameof(RefreshTokenService), nameof(ConsumeRefreshTokenAsync), removedToken.UserId);

        // Check if the removed token was already expired or resolved
        if (removedToken.IsResolved) 
        {
            logger.LogWarning("{ServiceName}.{MethodName} failed: Token removed, but it was already resolved/expired for User ID {UserId}.",
                nameof(RefreshTokenService), nameof(ConsumeRefreshTokenAsync), removedToken.UserId);

            return Result<RefreshToken>.Failure("Expired refresh token", HttpStatusCode.BadRequest);
        }

        return Result<RefreshToken>.Success(removedToken);
    }
}