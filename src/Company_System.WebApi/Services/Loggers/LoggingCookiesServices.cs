using System.Net;
using HR_System.Core.Abstraction.ServiceContracts;
using HR_System.Core.DTO.Token;
using HR_System.Core.Result;
using HR_System.Infrastructure.Decorators.Loggers;

namespace HR_System.Services.Loggers;

public class LoggingCookiesServices(
    ICookiesServices inner,
    ILogger<LoggingCookiesServices> logger) : ICookiesServices
{
    private const string ServiceName = nameof(CookiesServices);

    public Result Add(string key, string toAdd, int lifetimeInMinutes)
    {
        var result = inner.Add(key, toAdd, lifetimeInMinutes);
        // cookie values are deliberately not logged
        logger.LogResult(result, ServiceName, nameof(Add), $"CookieKey: {key}", logSuccess: false);
        return result;
    }

    public Result AddTokens(AccessAndRefreshTokenDTO tokens)
    {
        var result = inner.AddTokens(tokens);
        logger.LogResult(result, ServiceName, nameof(AddTokens), logSuccess: false);
        return result;
    }

    public Result<T> GetValue<T>(string key)
    {
        var result = inner.GetValue<T>(key);
        LogParseFailure(result, nameof(GetValue), key);
        return result;
    }

    public Result<string> Get(string key)
    {
        var result = inner.Get(key);
        LogParseFailure(result, nameof(Get), key);
        return result;
    }

    public Result<string> Remove(string key)
    {
        var result = inner.Remove(key);
        LogParseFailure(result, nameof(Remove), key);
        return result;
    }

    public Result<string> RemoveTokens() => inner.RemoveTokens();

    // A missing cookie is a normal situation (e.g. logged out), so only a
    // cookie that exists but can't be parsed (400) is worth logging.
    private void LogParseFailure(Result result, string methodName, string key)
    {
        if (result.StatusCode == HttpStatusCode.BadRequest)
            logger.LogResult(result, ServiceName, methodName, $"CookieKey: {key}");
    }
}
