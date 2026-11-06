namespace HR_System.Core.DTO.Attendance;

public class AttendanceEmployeeDTO
{
    public required Guid UserId { get; set; }

    public required string FullName { get; set; }

    public DailyWorkRecordDTO? Record { get; set; }
}
