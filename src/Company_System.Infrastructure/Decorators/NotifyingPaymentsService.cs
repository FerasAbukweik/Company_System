using HR_System.Core.Abstraction.ServiceContracts;
using HR_System.Core.DTO.Notification;
using HR_System.Core.DTO.Payments;
using HR_System.Core.Enums;
using HR_System.Core.Result;

namespace HR_System.Infrastructure.Decorators;

public class NotifyingPaymentsService(IPaymentsService inner, INotificationsService notifications) : IPaymentsService
{
    public async Task<Result<PaymentDTO>> UpdateStateAsync(Guid paymentId, PaymentStateEnum state, CancellationToken cancellationToken = default)
    {
        var result = await inner.UpdateStateAsync(paymentId, state, cancellationToken);
        if (!result.IsSuccess || state == PaymentStateEnum.Pending) return result;

        var notification = await NotifyAsync(result.Value!, cancellationToken);
        return notification.IsSuccess ? result : notification.MapFailure<PaymentDTO>();
    }

    public async Task<Result<IReadOnlyList<PaymentDTO>>> UpdateStatesAsync(IReadOnlyList<Guid> paymentIds, PaymentStateEnum state, CancellationToken cancellationToken = default)
    {
        var result = await inner.UpdateStatesAsync(paymentIds, state, cancellationToken);
        if (!result.IsSuccess) return result;

        foreach (var payment in result.Value!)
        {
            var notification = await NotifyAsync(payment, cancellationToken);
            if (!notification.IsSuccess) return notification.MapFailure<IReadOnlyList<PaymentDTO>>();
        }

        return result;
    }

    private Task<Result<NotificationDTO>> NotifyAsync(PaymentDTO payment, CancellationToken cancellationToken)
    {
        return notifications.AddAsync(new NotificationAddDTO
        {
            UserId = payment.UserId,
            Title = $"Payment {payment.State.ToString().ToLowerInvariant()}",
            Content = $"Your payment for {payment.Year}-{payment.Month:D2} was {payment.State.ToString().ToLowerInvariant()}.",
            TargetPath = "/my-portal",
            SourceKey = $"payment:{payment.Id}:{payment.State}",
            Type = NotificationTypeEnum.PaymentDecision
        }, cancellationToken);
    }

    public Task<Result<int>> GenerateMonthlyPaymentsAsync(DateOnly month, CancellationToken cancellationToken = default)
    {
        return inner.GenerateMonthlyPaymentsAsync(month, cancellationToken);
    }

    public Task<Result<IReadOnlyList<PaymentDTO>>> GetUserPaymentsAsync(Guid userId, int year, PaymentStateEnum? state = null, CancellationToken cancellationToken = default)
    {
        return inner.GetUserPaymentsAsync(userId, year, state, cancellationToken);
    }

    public Task<Result<AdminPaymentPageDTO>> GetPaymentsAsync(PaymentQueryDTO query, CancellationToken cancellationToken = default)
    {
        return inner.GetPaymentsAsync(query, cancellationToken);
    }

    public Task<Result<UserPaymentPageDTO>> GetUserPaymentPageAsync(Guid userId, UserPaymentQueryDTO query, CancellationToken cancellationToken = default)
    {
        return inner.GetUserPaymentPageAsync(userId, query, cancellationToken);
    }

}
