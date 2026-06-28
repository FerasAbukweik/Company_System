using System.ComponentModel.DataAnnotations;
using HR_System.Core.Enums;

namespace HR_System.Core.DTO.Approval;

public class ApprovalAddDTO
{
    [Required]
    public required ApprovalTypeEnum Type { get; set; }
    public Guid? TaskId { get; set; }
}