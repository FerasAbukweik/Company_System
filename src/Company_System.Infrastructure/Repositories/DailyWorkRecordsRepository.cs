using System.Linq.Expressions;
using HR_System.Core.DTO.Attendance;
using HR_System.Core.DTO.LazyLoading;
using HR_System.Core.Abstraction.RepositoryContracts;
using HR_System.Core.Domain.Entities;
using HR_System.Core.Enums;
using Microsoft.EntityFrameworkCore;

namespace HR_System.Infrastructure.Repositories;

public class DailyWorkRecordsRepository(
    ApplicationDbContext dbContext
    ) : IDailyWorkRecordsRepository
{
    public async Task<AttendanceDayPageDTO> GetDayRecordsAsync(
        DateOnly date,
        LazyDTO lazyData,
        CancellationToken cancellationToken = default)
    {
        var employees = dbContext.Users.AsNoTracking();
        var totalCount = await employees.CountAsync(cancellationToken);
        var items = await employees
            .OrderByDescending(user => user.CreatedAt)
            .ThenByDescending(user => user.Id)
            .Skip(lazyData.Taken)
            .Take(lazyData.SectionSize)
            .Select(user => new AttendanceEmployeeDTO
            {
                UserId = user.Id,
                FullName = user.FullName,
                Record = user.DailyWorkRecords
                    .Where(record => record.Date == date)
                    .Select(record => new DailyWorkRecordDTO
                    {
                        Id = record.Id,
                        UserId = record.UserId,
                        Date = record.Date,
                        MinutesWorked = record.MinutesWorked,
                        State = record.State
                    })
                    .FirstOrDefault()
            })
            .ToListAsync(cancellationToken);

        return new AttendanceDayPageDTO { Items = items, TotalCount = totalCount };
    }

    public void Add(DailyWorkRecord record, CancellationToken cancellationToken = default)
    {
        dbContext.DailyWorkRecords.Add(record);
    }

    // read only, so no tracking
    public async Task<DailyWorkRecord?> GetByIdAsync(Guid recordId, CancellationToken cancellationToken = default)
    {
        return await dbContext.DailyWorkRecords
            .AsNoTracking()
            .SingleOrDefaultAsync(e => e.Id == recordId, cancellationToken);
    }

    // read only, so no tracking
    public async Task<DailyWorkRecord?> GetByIdAsync(
        Guid recordId,
        Expression<Func<DailyWorkRecord, object?>>[] include,
        CancellationToken cancellationToken = default)
    {
        IQueryable<DailyWorkRecord> query = dbContext.DailyWorkRecords.AsNoTracking();

        foreach (var inc in include)
        {
            query = query.Include(inc);
        }

        return await query.SingleOrDefaultAsync(t => t.Id == recordId, cancellationToken);
    }

    public async Task<IReadOnlyList<DailyWorkRecord>> FilterAsync(
        Expression<Func<DailyWorkRecord, bool>> predicate,
        Expression<Func<DailyWorkRecord, object?>>[]? include = null,
        Expression<Func<DailyWorkRecord, object?>>? orderBy = null,
        bool orderByDesc = false,
        int? skip = null,
        int? take = null,
        CancellationToken cancellationToken = default)
    {
        return await Filter(predicate, include, orderBy, orderByDesc, skip, take).ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyDictionary<Guid, long>> GetWorkedMinutesAsync(
        DateOnly from,
        DateOnly toExclusive,
        CancellationToken cancellationToken = default)
    {
        return await dbContext.DailyWorkRecords
            .AsNoTracking()
            .Where(record => record.Date >= from && record.Date < toExclusive)
            .GroupBy(record => record.UserId)
            .Select(group => new { UserId = group.Key, Minutes = group.Sum(record => (long)record.MinutesWorked) })
            .ToDictionaryAsync(record => record.UserId, record => record.Minutes, cancellationToken);
    }

    public async Task<DailyWorkRecord?> UpdateAsync(
        Guid recordId,
        int minutesWorked,
        WorkDayStateEnum state,
        CancellationToken cancellationToken = default)
    {
        var record = await GetByIdAsync(recordId, cancellationToken);
        if (record is null) return null;

        dbContext.DailyWorkRecords.Attach(record);
        record.MinutesWorked = minutesWorked;
        record.State = state;

        dbContext.Entry(record).Property(r => r.MinutesWorked).IsModified = true;
        dbContext.Entry(record).Property(r => r.State).IsModified = true;

        // Nothing is written until the service calls SaveChangesAsync.
        return record;
    }

    private IQueryable<DailyWorkRecord> Filter(
        Expression<Func<DailyWorkRecord, bool>> predicate,
        Expression<Func<DailyWorkRecord, object?>>[]? include = null,
        Expression<Func<DailyWorkRecord, object?>>? orderBy = null,
        bool orderByDesc = false,
        int? skip = null,
        int? take = null
        )
    {
        var query = dbContext.DailyWorkRecords.AsNoTracking().Where(predicate);

        if (include != null)
        {
            foreach (var inc in include)
            {
                query = query.Include(inc);
            }
        }

        if (orderBy != null)
        {
            if (orderByDesc) query = query.OrderByDescending(orderBy);
            else query = query.OrderBy(orderBy);
        }

        if (skip != null) query = query.Skip(skip.Value);
        if (take != null) query = query.Take(take.Value);

        return query;
    }

    public async Task<bool> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return await dbContext.SaveChangesAsync(cancellationToken) > 0;
    }
}
