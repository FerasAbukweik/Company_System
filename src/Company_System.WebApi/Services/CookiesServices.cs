using System.ComponentModel;
using System.Net;
using HR_System.Core.Abstraction.ServiceContracts;
using HR_System.Core.DTO.Token;
using HR_System.Core.Options;
using HR_System.Core.Result;
using Microsoft.Extensions.Options;

namespace HR_System.Services;

public class CookiesServices(IHttpContextAccessor httpContextAccessor,
    IOptions<JWTOptions> jwtOptions,
    IOptions<CookieKeysOptions> cookieKeys) : ICookiesServices
{
    public Result Add(string key, string toAdd, int lifetimeInMinutes)
    {
        if (httpContextAccessor.HttpContext is null)
            return Result.Failure("HttpContext is null");
        
        var option = new CookieOptions()
        {
            HttpOnly = true,
            Secure = true,
            SameSite = SameSiteMode.Strict,
            Expires = DateTime.UtcNow.AddMinutes(lifetimeInMinutes)
        };
    
        httpContextAccessor.HttpContext.Response.Cookies.Append(key, toAdd, option);
    
        return Result.Success();
    }
    public Result AddTokens(AccessAndRefreshTokenDTO tokens)
    {
        var accessResult = Add(
            cookieKeys.Value.AccessToken,
            tokens.AccessToken,
            jwtOptions.Value.AccessTokenLifeTime
        );
    
        if (!accessResult.IsSuccess)
            return accessResult;
    
        return Add(
            cookieKeys.Value.RefreshToken,
            tokens.RefreshToken,
            jwtOptions.Value.RefreshTokenLifeTime
        );
    }
    public Result<T> GetValue<T>(string key)
    {
        if (httpContextAccessor.HttpContext is null)
            return Result<T>.Failure("HttpContext is null");

        if (!httpContextAccessor.HttpContext.Request.Cookies.TryGetValue(key, out string? value) 
            || string.IsNullOrWhiteSpace(value))
            return Result<T>.Failure("Cookie not found", HttpStatusCode.Unauthorized);

        try
        {
            var type = Nullable.GetUnderlyingType(typeof(T)) ?? typeof(T);
            var converter = TypeDescriptor.GetConverter(type);
            var parsed = (T?)converter.ConvertFromInvariantString(value);

            return parsed is not null
                ? Result<T>.Success(parsed)
                : Result<T>.Failure($"Cookie could not be converted to {typeof(T).Name}", HttpStatusCode.BadRequest);
        }
        catch (Exception)
        {
            return Result<T>.Failure($"Cookie could not be converted to {typeof(T).Name}", HttpStatusCode.BadRequest);
        }
    }
    public Result<string> Get(string key) => GetValue<string>(key);
    public Result<string> Remove(string key)
    {
        // get the value before removing it
        var removedResult = Get(key);

        // remove the value
        Add(key, "", -1);
        
        // return removed result
        // --if value doesnt exist this will return error
        return removedResult;
    }
    public Result<string> RemoveTokens()
    {
        var removeAccessTokenResult = Remove(cookieKeys.Value.AccessToken);
        var removeRefreshTokenResult = Remove(cookieKeys.Value.RefreshToken);

        return removeAccessTokenResult.IsSuccess ? removeRefreshTokenResult : removeAccessTokenResult;
    }
}