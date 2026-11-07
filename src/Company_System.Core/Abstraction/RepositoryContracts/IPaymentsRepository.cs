using HR_System.Core.Enums;
using System.Linq.Expressions;
using HR_System.Core.Domain.Entities;
using HR_System.Core.DTO.Payments;

namespace HR_System.Core.Abstraction.RepositoryContracts;

public interface IPaymentsRepository
{
    // Changes are staged until SaveChangesAsync is called.
    void Add(Payment payment, CancellationToken cancellationToken = default);

    // Read operations do not track entities.
    Task<Payment?> GetByIdAsync(Guid paymentId, CancellationToken cancellationToken = default);

    Task<Payment?> GetByIdAsync(
        Guid paymentId,
        Expression<Func<Payment, object?>>[] include,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Payment>> FilterAsync(
        Expression<Func<Payment, bool>> predicate,
        Expression<Func<Payment, object?>>[]? include = null,
        Expression<Func<Payment, object?>>? orderBy = null,
        bool orderByDesc = false,
        int? skip = null,
        int? take = null,
        CancellationToken cancellationToken = default);

    // Stages State and its approval PaymentDate; the service remains responsible for saving.
    Task<Payment?> UpdateStateAsync(
        Guid paymentId,
        PaymentStateEnum state,
        CancellationToken cancellationToken = default);

    void SetState(Payment payment, PaymentStateEnum state);

    Task<UserPaymentSummaryDTO> GetUserSummaryAsync(Guid userId, int year, CancellationToken cancellationToken = default);

    Task<int> CountAsync(Expression<Func<Payment, bool>> predicate, CancellationToken cancellationToken = default);

    Task<AdminPaymentSummaryDTO> GetAdminSummaryAsync(CancellationToken cancellationToken = default);

    Task<bool> SaveChangesAsync(CancellationToken cancellationToken = default);
}
