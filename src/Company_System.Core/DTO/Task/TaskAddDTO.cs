using System.ComponentModel.DataAnnotations;
using HR_System.Core.Enums;
using HR_System.Core.ValidationAttributes;

namespace HR_System.Core.DTO.Task;

public class TaskAddDTO
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
    
    
    // override

    public override string ToString()
    {
        return
            $"Title: {Title}\nDescription: {Description}\n" +
            $"\nDeadline: {Deadline}\nPriority:{Priority.ToString()}\n" +
            $"\nuser id: {UserId}\n";
    }
}