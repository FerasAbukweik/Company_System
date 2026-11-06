using System.Text.Json;

namespace HR_System.MiddleWares;

public class GlobalExceptionMiddleWare(RequestDelegate next,
    ILogger<GlobalExceptionMiddleWare> logger)
{
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await next(context);
        }

        catch (Exception ex)
        {
            logger.LogError(ex, ex.Message);

            if (context.Response.HasStarted)
                throw;

            context.Response.Clear();
            context.Response.StatusCode = 500;
            context.Response.ContentType = "application/json";

            var res = new
            {
                message = "An unexpected error occurred. Please try again later."
            };

            await context.Response.WriteAsync(JsonSerializer.Serialize(res), context.RequestAborted);
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