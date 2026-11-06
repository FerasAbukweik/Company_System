using HR_System.Core.Enums;

namespace HR_System.Core.DTO.Attendance;

public class DailyWorkRecordDTO
{
    public required Guid Id { get; set; }

    public required Guid UserId { get; set; }

    public required DateOnly Date { get; set; }

    public required int MinutesWorked { get; set; }

    public required WorkDayStateEnum State { get; set; }
}
