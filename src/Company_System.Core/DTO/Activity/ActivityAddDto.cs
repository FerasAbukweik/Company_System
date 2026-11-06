using HR_System.Core.Enums;
using HR_System.Core.ValidationAttributes;

namespace HR_System.Core.DTO.Activity;

public class ActivityAddDto
{
    public required ActivityTypeEnum Type { get; set; }
    
    [ExactlyOneOf("ApprovalId")]
    public Guid? TaskId { get; set; }
    public Guid? ApprovalId { get; set; }
}