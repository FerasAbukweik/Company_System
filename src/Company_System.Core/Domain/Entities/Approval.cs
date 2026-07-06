using System.ComponentModel.DataAnnotations;
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
    
    
    
    // relations
    
    public Guid? TaskId { get; set; }
    public AppTask? Task { get; set; }
    
    [Required]
    public required Guid UserRequestingId { get; set; }
    public ApplicationUser? UserRequesting { get; set; }
    
    [Required]
    public required Guid ManagerId { get; set; }
    public ApplicationUser? Manager { get; set; }
    
    public List<Activity> Activities { get; set; } = [];
    
    
    // functions
    public ToApproveDTO ToToApprovalDTO()
    {
        string body = Type switch
        {
            ApprovalTypeEnum.Holiday => "is requesting holiday",
            ApprovalTypeEnum.Task => $"has completed task: {Task?.Title ?? "missing take"}",
            _ => "unhandled task type"
        };
        
        return new ToApproveDTO()
        {
            Id = Id,
            CreatedOn = CreatedOn,
            RequesterName = "You",
            Body = body
        };
    }
    
    
    public RequestedApproval ToRequestedApprovalDTO()
    {
        string body = Type switch
        {
            ApprovalTypeEnum.Holiday => "is requesting holiday",
            ApprovalTypeEnum.Task => $"has completed task: {Task?.Title ?? "missing take"}",
            _ => "unhandled task type"
        };
        
        return new RequestedApproval()
        {
            Id = Id,
            CreatedOn = CreatedOn,
            RequesterName = UserRequesting?.UserName ?? "username not found",
            Body = body,
            Status = Status
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