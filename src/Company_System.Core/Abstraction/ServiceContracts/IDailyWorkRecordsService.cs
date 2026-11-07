using HR_System.Core.DTO.LazyLoading;
using HR_System.Core.DTO.Attendance;
using HR_System.Core.Enums;
using HR_System.Core.Result;

namespace HR_System.Core.Abstraction.ServiceContracts;

public interface IDailyWorkRecordsService
{
    Task<Result<AttendanceDayPageDTO>> GetDayRecordsAsync(
        DateOnly date,
        LazyDTO lazyData,
        CancellationToken cancellationToken = default);

    Task<Result<DailyWorkRecordDTO>> AddAsync(
        DailyWorkRecordSaveDTO data,
        CancellationToken cancellationToken = default);

    Task<Result<DailyWorkRecordDTO>> UpdateAsync(
        Guid recordId,
        TimeOnly? clockIn,
        TimeOnly? clockOut,
        WorkDayStateEnum state,
        CancellationToken cancellationToken = default);

    Task<Result<IReadOnlyList<DailyWorkRecordDTO>>> GetUserRecordsAsync(
        Guid userId,
        DateOnly from,
        DateOnly to,
        CancellationToken cancellationToken = default);
}
