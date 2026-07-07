using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using HR_System.Core.Domain.Identity;
using HR_System.Core.DTO.Activity;
using HR_System.Core.Enums;
using HR_System.Core.ValidationAttributes;

namespace HR_System.Core.Domain.Entities;

public class Activity
{
    public Guid Id { get; set; } = Guid.NewGuid();
    
    [Required]
    public required ActivityTypeEnum Type { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    [Column(TypeName = "nvarchar(50)")]
    public required string Title { get; set; }
    
    [Column(TypeName = "nvarchar(250)")]
    public required string Description { get; set; }


    
    // relations
    [Required]
    public required Guid TriggeredById { get; set; }
    public ApplicationUser? TriggeredBy { get; set; } 
    
    
    // functions
    
    public ActivityDTO ToDTO()
    {
        return new ActivityDTO()
        {
            Id = Id,
            CreatedAt = CreatedAt,
            Type = Type,
            Title = Title,
            Description = Description,
        };
    }
}