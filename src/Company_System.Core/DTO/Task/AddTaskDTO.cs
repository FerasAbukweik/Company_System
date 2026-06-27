using System.ComponentModel.DataAnnotations;
using HR_System.Core.Domain.Idnetity;
using HR_System.Core.ENUM;
using HR_System.Core.Enums;

namespace HR_System.Core.DTO.Task;

public class AddTaskDTO
{
        
    [Required]
    public required string Title { get; set; }
        
    [Required]
    public required string Description { get; set; }
    
    [Required]
    [NewDate]
    public required DateTime Deadline { get; set; }
        
    [Required]
    public required PrioritiesEnum Priority { get; set; }
        
    [Required]
    public required Guid UserId { get; set; }
}