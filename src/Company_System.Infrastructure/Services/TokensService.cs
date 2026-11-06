using System.Net;
using HR_System.Core.Abstraction.RepositoryContracts;
using HR_System.Core.Abstraction.ServiceContracts;
using HR_System.Core.Domain.Identity;
using HR_System.Core.DTO.Token;
using HR_System.Core.Options;
using HR_System.Core.Result;
using Microsoft.Extensions.Options;

namespace HR_System.Infrastructure.Services;

public class TokensService(
    IAccessTokenService accessTokenService,
    IRefreshTokenService refreshTokenService,
    ICookiesServices cookiesServices,
    IApplicationUsersRepository usersRepository,
    IOptions<CookieKeysOptions> cookieKeys) : ITokensService
{
    public async Task<Result<AccessAndRefreshTokenDTO>> GenerateNewTokensAsync(ApplicationUser user, CancellationToken cancellationToken = default)
    {

        // 1. Access Token
        var accessTokenResult = await accessTokenService.GenerateAccessTokenAsync(user);
        if (!accessTokenResult.IsSuccess)
            return accessTokenResult.MapFailure<AccessAndRefreshTokenDTO>();

        // 2. Refresh Token
        var refreshTokenResult = await refreshTokenService.GenerateRefreshTokenAsync(user.Id, cancellationToken);
        if (!refreshTokenResult.IsSuccess)
            return refreshTokenResult.MapFailure<AccessAndRefreshTokenDTO>();

        var result = new AccessAndRefreshTokenDTO
        {
            AccessToken = accessTokenResult.Value!.AccessToken!,
            RefreshToken = refreshTokenResult.Value!
        };


        return Result<AccessAndRefreshTokenDTO>.Success(result);
    }

    public async Task<Result<AccessAndRefreshTokenDTO>> RegenerateTokensAsync(CancellationToken cancellationToken = default)
    {
        // 1. Retrieve refresh token string from HTTP Cookies
        var cookieTokenResult = cookiesServices.Get(cookieKeys.Value.RefreshToken);
        if (!cookieTokenResult.IsSuccess)
            return cookieTokenResult.MapFailure<AccessAndRefreshTokenDTO>();

        // 2. Safely consume and remove token via RefreshTokenService
        var consumeResult = await refreshTokenService.ConsumeRefreshTokenAsync(cookieTokenResult.Value!, cancellationToken);
        if (!consumeResult.IsSuccess)
            return consumeResult.MapFailure<AccessAndRefreshTokenDTO>();

        var removedToken = consumeResult.Value!;

        // 3. Locate related security user profile
        var user = await usersRepository.GetByIdAsync(removedToken.UserId, [u => u.OrganizationHierarchy], cancellationToken);
        if (user == null)
            return Result<AccessAndRefreshTokenDTO>.Failure("User not found", HttpStatusCode.BadRequest);

        if (user.OrganizationHierarchy is { IsActive: false })
            return Result<AccessAndRefreshTokenDTO>.Failure("Account is inactive", HttpStatusCode.Unauthorized);

        // 4. Delegate generation of new pairs

        return await GenerateNewTokensAsync(user, cancellationToken);
    }

    public async Task<Result<AccessAndRefreshTokenDTO>> UpdateUserTokensAsync(CancellationToken cancellationToken = default)
    {
        // 1. Perform rotation sequence
        var newTokensResult = await RegenerateTokensAsync(cancellationToken);
        if (!newTokensResult.IsSuccess)
            return newTokensResult;

        // 2. Commit updated tokens directly back to the secure Client Cookies payload
        var cookieSaveResult = cookiesServices.AddTokens(newTokensResult.Value!);
        if (!cookieSaveResult.IsSuccess)
            return cookieSaveResult.MapFailure<AccessAndRefreshTokenDTO>();

        
        return newTokensResult;
    }
}