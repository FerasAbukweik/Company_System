using HR_System.Core.Enums;

namespace HR_System.Core.DTO.Activity;

public class ActivityAddDTO
{
    public required ActivityTypeEnum Type { get; set; }
    public Guid? TaskId { get; set; }
    public Guid? ApprovalId { get; set; }
}