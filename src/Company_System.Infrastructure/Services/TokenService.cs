using System.Net;
using HR_System.Core.common;
using HR_System.Core.Constraints;
using HR_System.Core.Domain.Identity;
using HR_System.Core.DTO.Token;
using HR_System.Core.Interfaces.ServiceContracts;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace HR_System.Infrastructure.Services;

public class TokensService(
    IAccessTokenService accessTokenService,
    IRefreshTokenService refreshTokenService,
    ICookiesServices cookiesServices,
    UserManager<ApplicationUser> userManager,
    IOptions<CookieKeys> cookieKeys,
    ILogger<TokensService> logger) : ITokensService
{
    public async Task<Result<AccessAndRefreshTokenDTO>> GenerateNewTokensAsync(ApplicationUser user, CancellationToken cancellationToken = default)
    {
        logger.LogInformation("{ServiceName}.{MethodName} initiation for User ID {UserId}.",
            nameof(TokensService), nameof(GenerateNewTokensAsync), user.Id);

        // 1. Access Token
        var accessTokenResult = await accessTokenService.GenerateAccessTokenAsync(user);
        if (!accessTokenResult.IsSuccess)
        {
            logger.LogWarning("{ServiceName}.{MethodName} delegation failed: AccessTokenService could not construct token for User ID {UserId}.",
                nameof(TokensService), nameof(GenerateNewTokensAsync), user.Id);
            return accessTokenResult.MapFailure<AccessAndRefreshTokenDTO>();
        }

        // 2. Refresh Token
        var refreshTokenResult = await refreshTokenService.GenerateRefreshTokenAsync(user.Id, cancellationToken);
        if (!refreshTokenResult.IsSuccess)
        {
            logger.LogWarning("{ServiceName}.{MethodName} delegation failed: RefreshTokenService could not generate/save token for User ID {UserId}.",
                nameof(TokensService), nameof(GenerateNewTokensAsync), user.Id);
            return refreshTokenResult.MapFailure<AccessAndRefreshTokenDTO>();
        }

        var result = new AccessAndRefreshTokenDTO
        {
            AccessToken = accessTokenResult.Value!,
            RefreshToken = refreshTokenResult.Value!
        };

        logger.LogInformation("{ServiceName}.{MethodName} successfully constructed both access and refresh token payloads for User ID {UserId}.",
            nameof(TokensService), nameof(GenerateNewTokensAsync), user.Id);

        return Result<AccessAndRefreshTokenDTO>.Success(result);
    }

    public async Task<Result<AccessAndRefreshTokenDTO>> RegenerateTokensAsync(CancellationToken cancellationToken = default)
    {
        // 1. Retrieve refresh token string from HTTP Cookies
        var cookieTokenResult = cookiesServices.Get(cookieKeys.Value.RefreshToken);
        if (!cookieTokenResult.IsSuccess)
        {
            logger.LogWarning("{ServiceName}.{MethodName} failed: Refresh token key is missing from incoming request cookies.",
                nameof(TokensService), nameof(RegenerateTokensAsync));
            return cookieTokenResult.MapFailure<AccessAndRefreshTokenDTO>();
        }

        // 2. Safely consume and remove token via RefreshTokenService
        var consumeResult = await refreshTokenService.ConsumeRefreshTokenAsync(cookieTokenResult.Value!, cancellationToken);
        if (!consumeResult.IsSuccess)
        {
            logger.LogWarning("{ServiceName}.{MethodName} failed: Token validation or revocation failed in RefreshTokenService.",
                nameof(TokensService), nameof(RegenerateTokensAsync));
            return consumeResult.MapFailure<AccessAndRefreshTokenDTO>();
        }

        var removedToken = consumeResult.Value!;

        // 3. Locate related security user profile
        var user = await userManager.FindByIdAsync(removedToken.UserId.ToString());
        if (user == null)
        {
            logger.LogError("{ServiceName}.{MethodName} security mismatch: Valid refresh token was owned by User ID {UserId}, but the user profile no longer exists in DB.",
                nameof(TokensService), nameof(RegenerateTokensAsync), removedToken.UserId);
            return Result<AccessAndRefreshTokenDTO>.Failure("User not found", HttpStatusCode.BadRequest);
        }

        // 4. Delegate generation of new pairs
        logger.LogInformation("{ServiceName}.{MethodName} generating replacement tokens for verified User ID {UserId}.",
            nameof(TokensService), nameof(RegenerateTokensAsync), user.Id);

        return await GenerateNewTokensAsync(user, cancellationToken);
    }

    public async Task<Result<AccessAndRefreshTokenDTO>> UpdateUserTokensAsync(CancellationToken cancellationToken = default)
    {
        // 1. Perform rotation sequence
        var newTokensResult = await RegenerateTokensAsync(cancellationToken);
        if (!newTokensResult.IsSuccess)
        {
            logger.LogWarning("{ServiceName}.{MethodName} aborted: Token regeneration cycle failed.",
                nameof(TokensService), nameof(UpdateUserTokensAsync));
            return newTokensResult;
        }

        // 2. Commit updated tokens directly back to the secure Client Cookies payload
        var cookieSaveResult = cookiesServices.AddTokens(newTokensResult.Value!);
        if (!cookieSaveResult.IsSuccess)
        {
            logger.LogError("{ServiceName}.{MethodName} failed to persist regenerated tokens into response cookies.",
                nameof(TokensService), nameof(UpdateUserTokensAsync));
            return cookieSaveResult.MapFailure<AccessAndRefreshTokenDTO>();
        }

        logger.LogInformation("{ServiceName}.{MethodName} successfully rotated tokens and committed updated values to HTTP response cookies.",
            nameof(TokensService), nameof(UpdateUserTokensAsync));

        return newTokensResult;
    }
}