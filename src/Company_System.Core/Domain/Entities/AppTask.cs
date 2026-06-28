using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;
using HR_System.Core.Domain.Identity;
using HR_System.Core.DTO.Task;
using HR_System.Core.Enums;

namespace HR_System.Core.Domain.Entities;

public class AppTask
{
    public Guid Id { get; set; } =  Guid.NewGuid();
    
    [Required]
    [Column(TypeName =  "Nvarchar(50)")]
    public required string Title { get; set; }
    
    [Required]
    [Column(TypeName =  "Nvarchar(500)")]
    public required string Description { get; set; }
    
    [Required]
    public required DateTime Created { get; set; }
    
    [Required]
    public required DateTime Deadline { get; set; }
    
    [Required]
    public required PrioritiesEnum Priority { get; set; }

    public TaskStatusEnum Status { get; set; } = TaskStatusEnum.Pending;
    
    
    // relations
    
    [Required]
    public required Guid UserId { get; set; }
    
    [JsonIgnore]
    public ApplicationUser? User { get; set; }
    
    
    [Required]
    public required Guid ManagerId { get; set; }
    
    [JsonIgnore]
    public ApplicationUser? Manager { get; set; }
    
    [JsonIgnore]
    public Approval? Approval { get; set; }
    
    [JsonIgnore]
    public List<Activity> Activities { get; set; } = [];
    
    
    
    // functions

    public TaskDTO ToDTO()
    {
        return new TaskDTO()
        {
            Id = Id,
            Title = Title,
            Description = Description,
            Created = Created,
            Deadline = Deadline,
            Priority = Priority,
            Status = Status,
            UserId = UserId,
            ManagerId = ManagerId
        };
    }
    
    
    // override

    public override string ToString()
    {
        return
            $"Id: {Id}\nTitle: {Title}\nDescription: {Description}\nCreated: {Created}" +
            $"\nDeadline: {Deadline}\nPriority:{Priority.ToString()}\nstatus: {Status}" +
            $"\nuserId: {UserId}\nmanagerId: {ManagerId}";
    }

    public override bool Equals(object? obj)
    {
        if (obj is not AppTask otherTask)
            return false;
        
        return Id == otherTask.Id &&
               Title == otherTask.Title &&
               Description == otherTask.Description &&
               Created == otherTask.Created &&
               Deadline == otherTask.Deadline &&
               Priority == otherTask.Priority && 
               Status == otherTask.Status &&
               UserId == otherTask.UserId &&
               ManagerId == otherTask.ManagerId;
    }
}