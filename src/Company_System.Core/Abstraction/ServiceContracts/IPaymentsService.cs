using HR_System.Core.Enums;
using HR_System.Core.DTO.Payments;
using HR_System.Core.Result;

namespace HR_System.Core.Abstraction.ServiceContracts;

public interface IPaymentsService
{
    /// <summary>Creates missing pending payments for the supplied month from active employees' hourly rates.</summary>
    Task<Result<int>> GenerateMonthlyPaymentsAsync(DateOnly month, CancellationToken cancellationToken = default);

    Task<Result<PaymentDTO>> UpdateStateAsync(
        Guid paymentId,
        PaymentStateEnum state,
        CancellationToken cancellationToken = default);

    Task<Result<IReadOnlyList<PaymentDTO>>> GetUserPaymentsAsync(
        Guid userId,
        int year,
        PaymentStateEnum? state = null,
        CancellationToken cancellationToken = default);
    Task<Result<AdminPaymentPageDTO>> GetPaymentsAsync(PaymentQueryDTO query, CancellationToken cancellationToken = default);

    Task<Result<IReadOnlyList<PaymentDTO>>> UpdateStatesAsync(IReadOnlyList<Guid> paymentIds, PaymentStateEnum state, CancellationToken cancellationToken = default);
    Task<Result<UserPaymentPageDTO>> GetUserPaymentPageAsync(Guid userId, UserPaymentQueryDTO query, CancellationToken cancellationToken = default);
}
