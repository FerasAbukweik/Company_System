using HR_System.Core.DTO;
using HR_System.Core.Helpers;
using HR_System.Core.ServiceContracts.ICookieServices;
using HR_System.Core.ServiceContracts.ITokenServices;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;

namespace HR_System.Core.Services.CookiesService;

public class AddCookieService : IAddCookieService
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly IGenerateTokenService _generateTokenService;
    private readonly IConfiguration _configuration;

    public AddCookieService(IHttpContextAccessor httpContextAccessor,
        IGenerateTokenService generateTokenService,
        IConfiguration configuration)
    {
        _httpContextAccessor = httpContextAccessor;
        _generateTokenService = generateTokenService;
        _configuration = configuration;
    }
    
    public Result AddToCookies(string key, string toAdd, int lifetimeInMinutes)
    {
        var option = new CookieOptions()
        {
            HttpOnly = true,
            Secure = true,
            SameSite = SameSiteMode.Strict,
            Expires = DateTime.UtcNow.AddMinutes(lifetimeInMinutes)
        };
        
        _httpContextAccessor.HttpContext?.Response.Cookies.Append(key, toAdd, option);
        
        return Result.Success();
    }
    public async Task<Result<AccessAndRefreshTokenDTO>> AddTokensToCookies(AccessAndRefreshTokenDTO? tokens = null)
    {
        if (tokens == null)
        {
            // generate new tokens based on current user
            var tokensResult = await _generateTokenService.GenerateNewAccessAndRefreshToken();
            if (!tokensResult.IsSuccess) return tokensResult;
            
            tokens = tokensResult.Value;
        }

        // add access token to cookies
        var addAccessTokenResult = this.AddToCookies("AccessToken", tokens!.AccessToken,
            _configuration.GetValue<int>("Jwt:AccessTokenLifeTime"));
        if (!addAccessTokenResult.IsSuccess) return addAccessTokenResult.MapFailure<AccessAndRefreshTokenDTO>();
        
        // add refresh token to cookies
        var addRefreshTokenResult = this.AddToCookies("RefreshToken", tokens!.RefreshToken, 
            _configuration.GetValue<int>("Jwt:RefreshTokenLifeTime"));
        if (!addRefreshTokenResult.IsSuccess) return addRefreshTokenResult.MapFailure<AccessAndRefreshTokenDTO>();
        
        
        return Result<AccessAndRefreshTokenDTO>.Success(tokens);
    }
}