using System.ComponentModel.DataAnnotations;
using System.Net.Http.Headers;
using HR_System.Core.Enums;

namespace HR_System.Core.DTO.Approval;

public class ApprovalAddDTO
{
    [Required]
    public required ApprovalTypeEnum Type { get; set; }
    public Guid? TaskId { get; set; }
    
    
    // override

    public override string ToString()
    {
        return $"Type: {Type.ToString()}\nTaskId: {TaskId}\n";
    }
}