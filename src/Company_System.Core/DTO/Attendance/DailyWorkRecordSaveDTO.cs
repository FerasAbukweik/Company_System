using System.ComponentModel.DataAnnotations;
using HR_System.Core.Enums;

namespace HR_System.Core.DTO.Attendance;

public class DailyWorkRecordSaveDTO
{
    [Required]
    public required Guid UserId { get; set; }

    [Required]
    public required DateOnly Date { get; set; }

    [Required]
    [Range(0, 1440)]
    public required int MinutesWorked { get; set; }

    [Required]
    [EnumDataType(typeof(WorkDayStateEnum))]
    public required WorkDayStateEnum State { get; set; }
}
