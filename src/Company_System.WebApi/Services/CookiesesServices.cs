using System.ComponentModel;
using System.Net;
using HR_System.Core.common;
using HR_System.Core.Constraints;
using HR_System.Core.DTO;
using HR_System.Core.DTO.Token;
using HR_System.Core.Interfaces.ServiceContracts.ICookieServices;

namespace HR_System.Services;

public class CookiesesServices(IHttpContextAccessor httpContextAccessor,
    IConfiguration configuration,
    ILogger<CookiesesServices> logger
    ) : ICookiesesServices
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
            CookieKeys.AccessToken,
            tokens.AccessToken,
            configuration.GetValue<int>("Jwt:AccessTokenLifeTime")
        );
    
        if (!accessResult.IsSuccess)
            return accessResult;
    
        return Add(
            CookieKeys.RefreshToken,
            tokens.RefreshToken,
            configuration.GetValue<int>("Jwt:RefreshTokenLifeTime")
        );
    }
    
    
    public Result<T> GetValue<T>(string key)
    {
        if (httpContextAccessor.HttpContext is null)
            return Result<T>.Failure("HttpContext is null");

        if (!httpContextAccessor.HttpContext.Request.Cookies.TryGetValue(key, out string? value) 
            || string.IsNullOrWhiteSpace(value))
            return Result<T>.Failure("Cookie not found", HttpStatusCode.OK);

        try
        {
            var type = Nullable.GetUnderlyingType(typeof(T)) ?? typeof(T);
            var converter = TypeDescriptor.GetConverter(type);
            var parsed = (T?)converter.ConvertFromInvariantString(value);

            return parsed is not null
                ? Result<T>.Success(parsed)
                : Result<T>.Failure($"Cookie could not be converted to {typeof(T).Name}", HttpStatusCode.BadRequest);
        }
        catch (Exception e)
        {
            logger.LogInformation($"Failed to parse {value} to type {typeof(T).Name}", HttpStatusCode.BadRequest);
            return Result<T>.Failure($"Cookie could not be converted to {typeof(T).Name}", HttpStatusCode.BadRequest);
        }
    }

    public Result<string> Get(string key) => GetValue<string>(key);
}