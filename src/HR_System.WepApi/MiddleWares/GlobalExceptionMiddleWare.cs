using System.Text.Json;

namespace HR_System.MiddleWares;

public class GlobalExceptionMiddleWare
{
    private readonly  RequestDelegate _next;
    private readonly  ILogger<GlobalExceptionMiddleWare> _logger;

    public GlobalExceptionMiddleWare(RequestDelegate next,
        ILogger<GlobalExceptionMiddleWare> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        
        catch (Exception ex)
        {
            _logger.LogError(ex, ex.Message);
            
            context.Response.StatusCode = 500;
            context.Response.ContentType = "application/json";

            var res = new
            {
                message = ex.Message + "|" + ex.InnerException?.Message ?? "no inner exceptoin" // temporary for test
            };

            await context.Response.WriteAsync(JsonSerializer.Serialize(res) , context.RequestAborted);
        }
    }
}

public static class GlobalExceptionMiddleWareExtensionMethods
{
    public static IApplicationBuilder UseGlobalExceptionMiddleWare(this IApplicationBuilder app)
    {
        return app.UseMiddleware<GlobalExceptionMiddleWare>();
    }
}