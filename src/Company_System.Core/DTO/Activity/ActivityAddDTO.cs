using HR_System.Core.Enums;
using HR_System.Core.ValidationAttributes;

namespace HR_System.Core.DTO.Activity;

public class ActivityAddDTO
{
    [CheckActivityType(nameof(TaskId),nameof(ApprovalId))]
    public required ActivityTypeEnum Type { get; set; }
    public Guid? TaskId { get; set; }
    public Guid? ApprovalId { get; set; }
    
    
    // override

    public override string ToString()
    {
        return $"Type: {Type.ToString()}\nTaskId: {TaskId}\nApprovalId: {ApprovalId}\n";
    }
}