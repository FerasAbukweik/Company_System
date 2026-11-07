using System.ComponentModel.DataAnnotations;
using HR_System.Core.Enums;

namespace HR_System.Core.DTO.Attendance;

public class DailyWorkRecordSaveDTO
{
    [Required]
    public required Guid UserId { get; set; }

    [Required]
    public required DateOnly Date { get; set; }

    public TimeOnly? ClockIn { get; set; }

    public TimeOnly? ClockOut { get; set; }

    [Required]
    [EnumDataType(typeof(WorkDayStateEnum))]
    public required WorkDayStateEnum State { get; set; }
}
