using HR_System.Services;
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
        var services = context.HttpContext.RequestServices;
        var dbContext = services.GetRequiredService<ApplicationDbContext>();
        var publisher = services.GetRequiredService<NotificationPublisher>();
        var cancellationToken = context.HttpContext.RequestAborted;
        var committed = false;
        var strategy = dbContext.Database.CreateExecutionStrategy();

        await strategy.ExecuteAsync(async () =>
        {
            committed = false;
            publisher.DiscardPending();
            await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);

            try
            {
                var executedContext = await next();

                if (executedContext.Exception is null && !IsFailureResult(executedContext.Result))
                {
                    await transaction.CommitAsync(cancellationToken);
                    committed = true;
                }
            }
            catch
            {
                publisher.DiscardPending();
                await transaction.RollbackAsync(CancellationToken.None);
                throw;
            }

            if (!committed)
            {
                publisher.DiscardPending();
                await transaction.RollbackAsync(CancellationToken.None);
            }
        });

        // Delivery failures must never roll back or fail an already committed operation.
        if (committed) await publisher.PublishPendingAsync();
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