using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using HR_System.Core.Domain.Identity;
using HR_System.Core.DTO.Approval;
using HR_System.Core.Enums;

namespace HR_System.Core.Domain.Entities;

public class Approval
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public ApprovalStatusEnum Status { get; set; } =  ApprovalStatusEnum.Pending;

    [Required]
    public required ApprovalTypeEnum Type { get; set; }

    public DateTime CreatedOn { get; set; } = DateTime.UtcNow;
    
    [Required]
    public required string Description { get; set; }
    
    
    
    // relations
    
    
    public Guid? TaskId { get; set; }
    
    [JsonIgnore]
    public AppTask? Task { get; set; }
    
    
    [Required]
    public required Guid UserRequestingId { get; set; }
    
    [JsonIgnore]
    public ApplicationUser? UserRequesting { get; set; }
    
    
    [Required]
    public required Guid ManagerId { get; set; }
    
    [JsonIgnore]
    public ApplicationUser? Manager { get; set; }
    
    [JsonIgnore]
    public List<Activity> Activities { get; set; } = [];
    
    
    // functions
    public ApprovalDTO ToDTO()
    {
        return new ApprovalDTO()
        {
            Id = Id,
            Status = Status,
            CreatedOn = CreatedOn,
            UserRequestingId = UserRequestingId,
            TaskId = TaskId,
            Type = Type,
            ManagerId = ManagerId,
        };
    }
    
    
    // override

    public override string ToString()
    {
        return $"Id: {Id}\nStatus: {Status}\nType: {Type}\n" +
               $"CreatedOn: {CreatedOn}\nTaskId: {TaskId}\n" +
               $"UserRequestingId: {UserRequestingId}\nManagerId: {ManagerId}\n";
    }

    public override bool Equals(object? obj)
    {
        if (obj is not Approval other)
            return false;
        
        return Id == other.Id && Status == other.Status && ManagerId == other.ManagerId &&
               Type == other.Type &&  CreatedOn == other.CreatedOn &&
               UserRequestingId == other.UserRequestingId && TaskId == other.TaskId;
    }
}