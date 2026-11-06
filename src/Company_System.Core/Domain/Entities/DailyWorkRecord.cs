using System.ComponentModel.DataAnnotations;
using HR_System.Core.Domain.Identity;
using HR_System.Core.DTO.Attendance;
using HR_System.Core.Enums;

namespace HR_System.Core.Domain.Entities;

public class DailyWorkRecord
{
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    public required DateOnly Date { get; set; }

    [Required]
    public required int MinutesWorked { get; set; }

    [Required]
    public required WorkDayStateEnum State { get; set; }


    // relations

    [Required]
    public required Guid UserId { get; set; }

    public ApplicationUser? User { get; set; }


    // functions

    public DailyWorkRecordDTO ToDTO()
    {
        return new DailyWorkRecordDTO()
        {
            Id = Id,
            UserId = UserId,
            Date = Date,
            MinutesWorked = MinutesWorked,
            State = State
        };
    }
}
