using HR_System.Core.Abstraction.ServiceContracts;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace HR_System.Infrastructure.BackGroundServices;

/// <summary>Generates payroll on startup and at each UTC calendar month's start. Runs are idempotent.</summary>
public class GenerateMonthlyPayments(
    IServiceScopeFactory serviceScopeFactory,
    ILogger<GenerateMonthlyPayments> logger) : BackgroundService
{
    private static readonly TimeSpan RetryInterval = TimeSpan.FromMinutes(5);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            var delay = RetryInterval;

            try
            {
                var month = GetPayrollMonth(DateTime.UtcNow);
                await using var scope = serviceScopeFactory.CreateAsyncScope();
                var paymentsService = scope.ServiceProvider.GetRequiredService<IPaymentsService>();
                var result = await paymentsService.GenerateMonthlyPaymentsAsync(month, stoppingToken);

                if (result.IsSuccess)
                {
                    var now = DateTime.UtcNow;
                    // If a long run crossed a month boundary, generate the new month immediately.
                    delay = GetPayrollMonth(now) == month
                        ? GetNextRunAt(now) - now
                        : TimeSpan.Zero;

                    logger.LogInformation("Monthly payroll generated for {Month}: {Created} pending records", month, result.Value);
                }
                else
                {
                    logger.LogWarning("Monthly payroll failed for {Month}: {Error}. Retrying in {RetryInterval}",
                        month, result.ErrorMessage, RetryInterval);
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception exception)
            {
                // Database downtime or a competing instance must not terminate the API host.
                logger.LogError(exception, "Monthly payroll generation failed; retrying in {RetryInterval}", RetryInterval);
            }

            try
            {
                await Task.Delay(delay, stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
        }
    }

    public static DateOnly GetPayrollMonth(DateTime utcNow)
    {
        return new DateOnly(utcNow.Year, utcNow.Month, 1).AddMonths(-1);
    }

    public static DateTime GetNextRunAt(DateTime utcNow)
    {
        return new DateTime(utcNow.Year, utcNow.Month, 1, 0, 0, 0, DateTimeKind.Utc).AddMonths(1);
    }
}
