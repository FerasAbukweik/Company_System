using System.Linq.Expressions;
using HR_System.Core.DTO.Attendance;
using HR_System.Core.DTO.LazyLoading;
using HR_System.Core.Domain.Entities;
using HR_System.Core.Enums;

namespace HR_System.Core.Abstraction.RepositoryContracts;

public interface IDailyWorkRecordsRepository
{
    Task<AttendanceDayPageDTO> GetDayRecordsAsync(
        DateOnly date,
        LazyDTO lazyData,
        CancellationToken cancellationToken = default);

    // Changes are staged until SaveChangesAsync is called.
    void Add(DailyWorkRecord record, CancellationToken cancellationToken = default);

    // Read operations do not track entities.
    Task<DailyWorkRecord?> GetByIdAsync(Guid recordId, CancellationToken cancellationToken = default);

    Task<DailyWorkRecord?> GetByIdAsync(
        Guid recordId,
        Expression<Func<DailyWorkRecord, object?>>[] include,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<DailyWorkRecord>> FilterAsync(
        Expression<Func<DailyWorkRecord, bool>> predicate,
        Expression<Func<DailyWorkRecord, object?>>[]? include = null,
        Expression<Func<DailyWorkRecord, object?>>? orderBy = null,
        bool orderByDesc = false,
        int? skip = null,
        int? take = null,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyDictionary<Guid, decimal>> GetWorkedHoursAsync(
        DateOnly from,
        DateOnly toExclusive,
        CancellationToken cancellationToken = default);

    // Employee and date remain fixed; only clock times and state are modified.
    Task<DailyWorkRecord?> UpdateAsync(
        Guid recordId,
        TimeOnly? clockIn,
        TimeOnly? clockOut,
        WorkDayStateEnum state,
        CancellationToken cancellationToken = default);

    Task<bool> SaveChangesAsync(CancellationToken cancellationToken = default);
}
