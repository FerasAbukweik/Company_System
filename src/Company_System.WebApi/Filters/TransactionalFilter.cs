using HR_System.Core.Abstraction.ServiceContracts;
using HR_System.Infrastructure;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace HR_System.Filters;

public class TransactionalAttribute : ActionFilterAttribute
{
    public override async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        // DI database
        var dbContext = context.HttpContext.RequestServices.GetRequiredService<ApplicationDbContext>();

        var publisher = context.HttpContext.RequestServices.GetService<INotificationPublisher>();
        var committed = false;

        var strategy = dbContext.Database.CreateExecutionStrategy();

        await strategy.ExecuteAsync(async () =>
        {
            publisher?.Clear();
            await using var transaction = await dbContext.Database.BeginTransactionAsync();
            try
            {
                var executedContext = await next();

                if (executedContext.Exception != null || IsFailureResult(executedContext.Result))
                    await transaction.RollbackAsync();
                
                else
                {
                    await transaction.CommitAsync();
                    committed = true;
                }
            }
            catch (Exception)
            {
                await transaction.RollbackAsync();
                throw;
            }
        });

        if (committed && publisher is not null) await publisher.FlushAsync();
        else publisher?.Clear();
    }
    
    
    private static bool IsFailureResult(IActionResult? result)
    {
        if (result == null) return false;

        // Handles standard status-bearing results like BadRequestObjectResult, ObjectResult, etc.
        if (result is IStatusCodeActionResult statusCodeResult)
        {
            var statusCode = statusCodeResult.StatusCode;
            return statusCode.HasValue && statusCode.Value >= 400;
        }

        // Fallback for custom ObjectResults that don't implement the interface directly
        if (result is ObjectResult objectResult)
        {
            return objectResult.StatusCode.HasValue && objectResult.StatusCode.Value >= 400;
        }

        return false;
    }
}