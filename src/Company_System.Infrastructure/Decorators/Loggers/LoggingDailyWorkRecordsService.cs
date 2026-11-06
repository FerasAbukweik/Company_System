using HR_System.Core.DTO.LazyLoading;
using HR_System.Core.Abstraction.ServiceContracts;
using HR_System.Core.DTO.Attendance;
using HR_System.Core.Enums;
using HR_System.Core.Result;
using HR_System.Infrastructure.Services;
using Microsoft.Extensions.Logging;

namespace HR_System.Infrastructure.Decorators.Loggers;

public class LoggingDailyWorkRecordsService(
    IDailyWorkRecordsService inner,
    ILogger<LoggingDailyWorkRecordsService> logger) : IDailyWorkRecordsService
{
    private const string ServiceName = nameof(DailyWorkRecordsService);

    public async Task<Result<AttendanceDayPageDTO>> GetDayRecordsAsync(
        DateOnly date,
        LazyDTO lazyData,
        CancellationToken cancellationToken = default)
    {
        var result = await inner.GetDayRecordsAsync(date, lazyData, cancellationToken);

        logger.LogResult(result, ServiceName, nameof(GetDayRecordsAsync),
            $"Date: {date}, {lazyData}", logSuccess: false);

        return result;
    }

    public async Task<Result<DailyWorkRecordDTO>> AddAsync(
        DailyWorkRecordSaveDTO data,
        CancellationToken cancellationToken = default)
    {
        var result = await inner.AddAsync(data, cancellationToken);

        logger.LogResult(result, ServiceName, nameof(AddAsync),
            $"UserId: {data.UserId}, Date: {data.Date}, RecordId: {result.Value?.Id}");

        return result;
    }

    public async Task<Result<DailyWorkRecordDTO>> UpdateAsync(
        Guid recordId,
        int minutesWorked,
        WorkDayStateEnum state,
        CancellationToken cancellationToken = default)
    {
        var result = await inner.UpdateAsync(recordId, minutesWorked, state, cancellationToken);

        logger.LogResult(result, ServiceName, nameof(UpdateAsync),
            $"RecordId: {recordId}, State: {state}");

        return result;
    }

    public async Task<Result<IReadOnlyList<DailyWorkRecordDTO>>> GetUserRecordsAsync(
        Guid userId,
        DateOnly from,
        DateOnly to,
        CancellationToken cancellationToken = default)
    {
        var result = await inner.GetUserRecordsAsync(userId, from, to, cancellationToken);

        logger.LogResult(result, ServiceName, nameof(GetUserRecordsAsync),
            $"UserId: {userId}, From: {from}, To: {to}", logSuccess: false);

        return result;
    }
}
