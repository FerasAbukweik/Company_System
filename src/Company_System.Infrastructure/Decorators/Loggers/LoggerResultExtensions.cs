using HR_System.Core.Result;
using Microsoft.Extensions.Logging;

namespace HR_System.Infrastructure.Decorators.Loggers;

/// <summary>
/// Shared logging helper used by the logging decorators.
/// Success is logged as Information, 5xx failures as Error and every other failure as Warning.
/// </summary>
public static class LoggerResultExtensions
{
    public static void LogResult(
        this ILogger logger,
        Result result,
        string serviceName,
        string methodName,
        string context = "",
        bool logSuccess = true)
    {
        if (result.IsSuccess)
        {
            if (logSuccess)
            {
                logger.LogInformation("{ServiceName}.{MethodName} succeeded. {Context}",
                    serviceName, methodName, context);
            }
            return;
        }

        var level = (int)result.StatusCode >= 500 ? LogLevel.Error : LogLevel.Warning;
        logger.Log(level, "{ServiceName}.{MethodName} failed with {StatusCode}: {ErrorMessage}. {Context}",
            serviceName, methodName, result.StatusCode, result.ErrorMessage, context);
    }
}
