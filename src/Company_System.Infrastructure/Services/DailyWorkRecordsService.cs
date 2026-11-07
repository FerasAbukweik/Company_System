using HR_System.Core.DTO.LazyLoading;
using System.Net;
using HR_System.Core.Abstraction.RepositoryContracts;
using HR_System.Core.Abstraction.ServiceContracts;
using HR_System.Core.Domain.Entities;
using HR_System.Core.DTO.Attendance;
using HR_System.Core.Enums;
using HR_System.Core.Result;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace HR_System.Infrastructure.Services;

public class DailyWorkRecordsService(
    IDailyWorkRecordsRepository recordsRepository,
    IApplicationUsersRepository usersRepository) : IDailyWorkRecordsService
{
    public async Task<Result<AttendanceDayPageDTO>> GetDayRecordsAsync(
        DateOnly date,
        LazyDTO lazyData,
        CancellationToken cancellationToken = default)
    {
        if (date == default || lazyData.Taken < 0 || lazyData.SectionSize < 1)
            return Result<AttendanceDayPageDTO>.Failure("Invalid attendance date or page size", HttpStatusCode.BadRequest);

        var page = await recordsRepository.GetDayRecordsAsync(date, lazyData, cancellationToken);

        return Result<AttendanceDayPageDTO>.Success(page);
    }

    public async Task<Result<DailyWorkRecordDTO>> AddAsync(
        DailyWorkRecordSaveDTO data,
        CancellationToken cancellationToken = default)
    {
        if (data.UserId == Guid.Empty || data.Date == default || !Enum.IsDefined(data.State))
            return Result<DailyWorkRecordDTO>.Failure("Invalid daily work record", HttpStatusCode.BadRequest);

        var user = await usersRepository.GetByIdAsync(data.UserId, cancellationToken);
        if (user is null)
            return Result<DailyWorkRecordDTO>.Failure("Employee not found", HttpStatusCode.NotFound);

        var existing = await recordsRepository.FilterAsync(
            r => r.UserId == data.UserId && r.Date == data.Date,
            take: 1,
            cancellationToken: cancellationToken);

        if (existing.Count > 0)
            return Result<DailyWorkRecordDTO>.Failure("A record already exists for this employee and date", HttpStatusCode.Conflict);

        var record = new DailyWorkRecord()
        {
            UserId = data.UserId,
            Date = data.Date,
            ClockIn = data.ClockIn,
            ClockOut = data.ClockOut,
            State = data.State
        };

        recordsRepository.Add(record, cancellationToken);

        try
        {
            if (!await recordsRepository.SaveChangesAsync(cancellationToken))
                return Result<DailyWorkRecordDTO>.Failure("Failed to add daily work record");
        }
        catch (DbUpdateException exception) when (exception.InnerException is SqlException { Number: 2601 or 2627 })
        {
            // A concurrent request may have inserted the same employee/date after the check.
            return Result<DailyWorkRecordDTO>.Failure("A record already exists for this employee and date", HttpStatusCode.Conflict);
        }

        return Result<DailyWorkRecordDTO>.Success(record.ToDTO(), HttpStatusCode.Created);
    }

    public async Task<Result<DailyWorkRecordDTO>> UpdateAsync(
        Guid recordId,
        TimeOnly? clockIn,
        TimeOnly? clockOut,
        WorkDayStateEnum state,
        CancellationToken cancellationToken = default)
    {
        if (!Enum.IsDefined(state))
            return Result<DailyWorkRecordDTO>.Failure("Invalid clock-in, clock-out or day state", HttpStatusCode.BadRequest);

        var record = await recordsRepository.UpdateAsync(recordId, clockIn, clockOut, state, cancellationToken);
        if (record is null)
            return Result<DailyWorkRecordDTO>.Failure("Daily work record not found", HttpStatusCode.NotFound);

        if (!await recordsRepository.SaveChangesAsync(cancellationToken))
            return Result<DailyWorkRecordDTO>.Failure("Failed to update daily work record");

        return Result<DailyWorkRecordDTO>.Success(record.ToDTO());
    }

    public async Task<Result<IReadOnlyList<DailyWorkRecordDTO>>> GetUserRecordsAsync(
        Guid userId,
        DateOnly from,
        DateOnly to,
        CancellationToken cancellationToken = default)
    {
        if (userId == Guid.Empty || from == default || to == default || from > to)
            return Result<IReadOnlyList<DailyWorkRecordDTO>>.Failure("Invalid employee or date range", HttpStatusCode.BadRequest);

        // Bound each query to one year; clients can request a month for the calendar.
        if (to.DayNumber - from.DayNumber > 365)
            return Result<IReadOnlyList<DailyWorkRecordDTO>>.Failure("Date range cannot exceed one year", HttpStatusCode.BadRequest);

        var records = await recordsRepository.FilterAsync(
            r => r.UserId == userId && r.Date >= from && r.Date <= to,
            orderBy: r => r.Date,
            orderByDesc: true,
            cancellationToken: cancellationToken);

        return Result<IReadOnlyList<DailyWorkRecordDTO>>.Success(records.Select(r => r.ToDTO()).ToList());
    }

}
