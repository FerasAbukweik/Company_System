using System.Linq.Expressions;
using HR_System.Core.Abstraction.RepositoryContracts;
using HR_System.Core.Domain.Identity;
using HR_System.Core.DTO.Users;
using HR_System.Core.Domain.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace HR_System.Infrastructure.Repositories;

public class ApplicationUsersRepository(
    ApplicationDbContext dbContext,
    ILookupNormalizer lookupNormalizer
    ) : IApplicationUsersRepository
{
    // read only, so no tracking
    public async Task<ApplicationUser?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await dbContext.Users
            .AsNoTracking()
            .SingleOrDefaultAsync(e => e.Id == id, cancellationToken);
    }

    // read only, so no tracking
    public async Task<ApplicationUser?> GetByIdAsync(
        Guid id,
        Expression<Func<ApplicationUser, object?>>[] include,
        CancellationToken cancellationToken = default)
    {
        IQueryable<ApplicationUser> query = dbContext.Users.AsNoTracking();

        foreach (var inc in include)
        {
            query = query.Include(inc);
        }

        return await query.SingleOrDefaultAsync(e => e.Id == id, cancellationToken);
    }

    // read only, so no tracking
    // (the lockout counters are written by UserManager, which attaches the user itself before saving)
    public async Task<ApplicationUser?> GetByEmailAsync(
        string email,
        Expression<Func<ApplicationUser, object?>>[]? include = null,
        CancellationToken cancellationToken = default)
    {
        // Identity stores and indexes the normalized form, so match against that
        var normalizedEmail = lookupNormalizer.NormalizeEmail(email);

        IQueryable<ApplicationUser> query = dbContext.Users.AsNoTracking();

        if (include != null)
        {
            foreach (var inc in include)
            {
                query = query.Include(inc);
            }
        }

        return await query.FirstOrDefaultAsync(u => u.NormalizedEmail == normalizedEmail, cancellationToken);
    }

    public async Task<IReadOnlyList<ApplicationUser>> FilterAsync(
        Expression<Func<ApplicationUser, bool>> predicate,
        Expression<Func<ApplicationUser, object?>>[]? include = null,
        Expression<Func<ApplicationUser, object?>>? orderBy = null,
        bool orderByDesc = false,
        int? skip = null,
        int? take = null,
        CancellationToken cancellationToken = default)
    {
        return await Filter(predicate, include, orderBy, orderByDesc, skip, take).ToListAsync(cancellationToken);
    }

    private IQueryable<ApplicationUser> Filter(
        Expression<Func<ApplicationUser, bool>> predicate,
        Expression<Func<ApplicationUser, object?>>[]? include = null,
        Expression<Func<ApplicationUser, object?>>? orderBy = null,
        bool orderByDesc = false,
        int? skip = null,
        int? take = null
        )
    {
        var query = dbContext.Users.AsNoTracking().Where(predicate);

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
            query = ordered.ThenByDescending(user => user.Id);
        }

        if (skip != null) query = query.Skip(skip.Value);
        if (take != null) query = query.Take(take.Value);

        return query;
    }

    public async Task<EmployeeSummaryDTO> GetSummaryAsync(Expression<Func<ApplicationUser, bool>> predicate, CancellationToken cancellationToken = default)
    {
        return await dbContext.Users.AsNoTracking().Where(predicate)
            .GroupBy(user => 1)
            .Select(group => new EmployeeSummaryDTO
            {
                Headcount = group.Count(),
                Active = group.Count(user => user.OrganizationHierarchy != null && user.OrganizationHierarchy.IsActive),
                TotalHourlyRates = group.Sum(user => user.HourlyRate ?? 0),
                ConfiguredHourlyRates = group.Count(user => user.HourlyRate != null)
            })
            .SingleOrDefaultAsync(cancellationToken) ?? new EmployeeSummaryDTO();
    }

    public async Task ReplaceImageAsync(Guid userId, Image image, CancellationToken cancellationToken = default)
    {
        var previous = await dbContext.Images.SingleOrDefaultAsync(i => i.UserId == userId, cancellationToken);
        if (previous is not null) previous.UserId = null;

        image.UserId = userId;
        dbContext.Images.Add(image);
    }

    public async Task<bool> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return await dbContext.SaveChangesAsync(cancellationToken) > 0;
    }
}
