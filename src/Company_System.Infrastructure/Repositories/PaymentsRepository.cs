using HR_System.Core.Enums;
using System.Linq.Expressions;
using HR_System.Core.Abstraction.RepositoryContracts;
using HR_System.Core.Domain.Entities;
using HR_System.Core.DTO.Payments;
using Microsoft.EntityFrameworkCore;

namespace HR_System.Infrastructure.Repositories;

public class PaymentsRepository(
    ApplicationDbContext dbContext
    ) : IPaymentsRepository
{
    public void Add(Payment payment, CancellationToken cancellationToken = default)
    {
        dbContext.Payments.Add(payment);
    }

    // read only, so no tracking
    public async Task<Payment?> GetByIdAsync(Guid paymentId, CancellationToken cancellationToken = default)
    {
        return await dbContext.Payments
            .AsNoTracking()
            .SingleOrDefaultAsync(e => e.Id == paymentId, cancellationToken);
    }

    // read only, so no tracking
    public async Task<Payment?> GetByIdAsync(
        Guid paymentId,
        Expression<Func<Payment, object?>>[] include,
        CancellationToken cancellationToken = default)
    {
        IQueryable<Payment> query = dbContext.Payments.AsNoTracking();

        foreach (var inc in include)
        {
            query = query.Include(inc);
        }

        return await query.SingleOrDefaultAsync(t => t.Id == paymentId, cancellationToken);
    }

    public async Task<IReadOnlyList<Payment>> FilterAsync(
        Expression<Func<Payment, bool>> predicate,
        Expression<Func<Payment, object?>>[]? include = null,
        Expression<Func<Payment, object?>>? orderBy = null,
        bool orderByDesc = false,
        int? skip = null,
        int? take = null,
        CancellationToken cancellationToken = default)
    {
        return await Filter(predicate, include, orderBy, orderByDesc, skip, take).ToListAsync(cancellationToken);
    }

    public async Task<Payment?> UpdateStateAsync(
        Guid paymentId,
        PaymentStateEnum state,
        CancellationToken cancellationToken = default)
    {
        var payment = await GetByIdAsync(paymentId, cancellationToken);
        if (payment is null) return null;

        SetState(payment, state);

        return payment;
    }

    public void SetState(Payment payment, PaymentStateEnum state)
    {
        dbContext.Payments.Attach(payment);
        if (state != PaymentStateEnum.Approved)
            payment.PaymentDate = null;
        else if (payment.State != PaymentStateEnum.Approved || payment.PaymentDate is null)
            payment.PaymentDate = DateOnly.FromDateTime(DateTime.UtcNow);

        payment.State = state;

        dbContext.Entry(payment).Property(p => p.State).IsModified = true;
        dbContext.Entry(payment).Property(p => p.PaymentDate).IsModified = true;
    }

    private IQueryable<Payment> Filter(
        Expression<Func<Payment, bool>> predicate,
        Expression<Func<Payment, object?>>[]? include = null,
        Expression<Func<Payment, object?>>? orderBy = null,
        bool orderByDesc = false,
        int? skip = null,
        int? take = null
        )
    {
        var query = dbContext.Payments.AsNoTracking().Where(predicate);

        if (include != null)
        {
            foreach (var inc in include)
            {
                query = query.Include(inc);
            }
        }

        if (orderBy != null)
        {
            var ordered = orderByDesc
                ? query.OrderByDescending(orderBy)
                : query.OrderBy(orderBy);
            query = ordered.ThenByDescending(payment => payment.Id);
        }

        if (skip != null) query = query.Skip(skip.Value);
        if (take != null) query = query.Take(take.Value);

        return query;
    }

    public async Task<UserPaymentSummaryDTO> GetUserSummaryAsync(Guid userId, int year, PaymentStateEnum? state, CancellationToken cancellationToken = default)
    {
        return await dbContext.Payments.AsNoTracking().Where(payment => payment.UserId == userId && payment.Year == year)
            .GroupBy(payment => 1)
            .Select(group => new UserPaymentSummaryDTO
            {
                TotalCount = group.Count(payment => !state.HasValue || payment.State == state.Value),
                ApprovedAmount = group.Sum(payment => payment.State == PaymentStateEnum.Approved ? payment.PaidAmount : 0),
                PendingAmount = group.Sum(payment => payment.State == PaymentStateEnum.Pending ? payment.PaidAmount : 0)
            })
            .SingleOrDefaultAsync(cancellationToken) ?? new UserPaymentSummaryDTO();
    }

    public async Task<AdminPaymentSummaryDTO> GetAdminSummaryAsync(PaymentQueryDTO query, CancellationToken cancellationToken = default)
    {
        var name = query.FullName?.Trim().ToUpperInvariant();
        return await dbContext.Payments.AsNoTracking().Where(payment => payment.Year == query.Year &&
                (!query.Month.HasValue || payment.Month == query.Month) &&
                (!query.UserId.HasValue || payment.UserId == query.UserId) &&
                (string.IsNullOrEmpty(name) || (payment.User != null && payment.User.FullName.ToUpper().Contains(name))))
            .GroupBy(payment => 1)
            .Select(group => new AdminPaymentSummaryDTO
            {
                TotalCount = group.Count(),
                PendingCount = group.Count(payment => payment.State == PaymentStateEnum.Pending),
                ApprovedCount = group.Count(payment => payment.State == PaymentStateEnum.Approved),
                RejectedCount = group.Count(payment => payment.State == PaymentStateEnum.Rejected),
                PendingAmount = group.Sum(payment => payment.State == PaymentStateEnum.Pending ? payment.PaidAmount : 0),
                ApprovedAmount = group.Sum(payment => payment.State == PaymentStateEnum.Approved ? payment.PaidAmount : 0),
                RejectedAmount = group.Sum(payment => payment.State == PaymentStateEnum.Rejected ? payment.PaidAmount : 0)
            })
            .SingleOrDefaultAsync(cancellationToken) ?? new AdminPaymentSummaryDTO();
    }

    public async Task<bool> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return await dbContext.SaveChangesAsync(cancellationToken) > 0;
    }
}
