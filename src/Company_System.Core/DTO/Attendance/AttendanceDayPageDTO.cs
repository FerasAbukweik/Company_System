namespace HR_System.Core.DTO.Attendance;

public class AttendanceDayPageDTO
{
    public required IReadOnlyList<AttendanceEmployeeDTO> Items { get; set; }

    public required int TotalCount { get; set; }
}
