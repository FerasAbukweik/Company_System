using HR_System.Core.Enums;
using HR_System.Core.Abstraction.ServiceContracts;
using HR_System.Core.DTO.Payments;
using HR_System.Core.Result;
using HR_System.Infrastructure.Services;
using Microsoft.Extensions.Logging;

namespace HR_System.Infrastructure.Decorators.Loggers;

public class LoggingPaymentsService(
    IPaymentsService inner,
    ILogger<LoggingPaymentsService> logger) : IPaymentsService
{
    private const string ServiceName = nameof(PaymentsService);

    public async Task<Result<int>> GenerateMonthlyPaymentsAsync(DateOnly month, CancellationToken cancellationToken = default)
    {
        var result = await inner.GenerateMonthlyPaymentsAsync(month, cancellationToken);

        logger.LogResult(result, ServiceName, nameof(GenerateMonthlyPaymentsAsync),
            $"Month: {month:yyyy-MM}, Created: {result.Value}");

        return result;
    }

    public async Task<Result<PaymentDTO>> UpdateStateAsync(
        Guid paymentId,
        PaymentStateEnum state,
        CancellationToken cancellationToken = default)
    {
        var result = await inner.UpdateStateAsync(paymentId, state, cancellationToken);

        logger.LogResult(result, ServiceName, nameof(UpdateStateAsync),
            $"PaymentId: {paymentId}, State: {state}");

        return result;
    }

    public async Task<Result<IReadOnlyList<PaymentDTO>>> GetUserPaymentsAsync(
        Guid userId,
        int year,
        PaymentStateEnum? state = null,
        CancellationToken cancellationToken = default)
    {
        var result = await inner.GetUserPaymentsAsync(userId, year, state, cancellationToken);

        logger.LogResult(result, ServiceName, nameof(GetUserPaymentsAsync),
            $"UserId: {userId}, Year: {year}, State: {state?.ToString() ?? "All"}", logSuccess: false);

        return result;
    }
    public async Task<Result<AdminPaymentPageDTO>> GetPaymentsAsync(PaymentQueryDTO query, CancellationToken cancellationToken = default)
    {
        var result = await inner.GetPaymentsAsync(query, cancellationToken);
        logger.LogResult(result, ServiceName, nameof(GetPaymentsAsync), $"Year: {query.Year}, State: {query.State}, Taken: {query.Taken}", logSuccess: false);
        return result;
    }

    public async Task<Result<IReadOnlyList<PaymentDTO>>> UpdateStatesAsync(IReadOnlyList<Guid> paymentIds, PaymentStateEnum state, CancellationToken cancellationToken = default)
    {
        var result = await inner.UpdateStatesAsync(paymentIds, state, cancellationToken);
        logger.LogResult(result, ServiceName, nameof(UpdateStatesAsync), $"Count: {paymentIds.Count}, State: {state}");
        return result;
    }
    public async Task<Result<UserPaymentPageDTO>> GetUserPaymentPageAsync(Guid userId, UserPaymentQueryDTO query, CancellationToken cancellationToken = default)
    {
        var result = await inner.GetUserPaymentPageAsync(userId, query, cancellationToken);
        logger.LogResult(result, ServiceName, nameof(GetUserPaymentPageAsync),
            $"UserId: {userId}, Year: {query.Year}, State: {query.State}, Taken: {query.Taken}, SectionSize: {query.SectionSize}", logSuccess: false);
        return result;
    }
    public async Task<Result<UserPaymentOverviewDTO>> GetUserOverviewAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var result = await inner.GetUserOverviewAsync(userId, cancellationToken);
        logger.LogResult(result, ServiceName, nameof(GetUserOverviewAsync), $"UserId: {userId}", logSuccess: false);

        return result;
    }

    public async Task<Result<AdminPaymentSummaryDTO>> GetAdminSummaryAsync(CancellationToken cancellationToken = default)
    {
        var result = await inner.GetAdminSummaryAsync(cancellationToken);
        logger.LogResult(result, ServiceName, nameof(GetAdminSummaryAsync), logSuccess: false);

        return result;
    }

}
