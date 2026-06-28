using System.ComponentModel.DataAnnotations;
using HR_System.Core.Domain.Identity;
using HR_System.Core.DTO.Activity;
using HR_System.Core.Enums;
using HR_System.Core.ValidationAttributes;

namespace HR_System.Core.Domain.Entities;

public class Activity
{
    public Guid Id { get; set; } = Guid.NewGuid();
    
    [Required]
    [CheckActivityType(nameof(TaskId), nameof(ApprovalId))]
    public required ActivityTypeEnum Type { get; set; }
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;


    
    
    // relations
    public Guid? TaskId { get; set; }
    
    public AppTask? Task { get; set; }
    
    
    public Guid? ApprovalId { get; set; }
    
    public Approval? Approval { get; set; }
    
    
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
            Title = GenerateTitle(),
            Description = GenerateDescription(),
            Name = TriggeredBy?.UserName ?? ""
        };
    }
    
    
    private bool IsTask(ActivityTypeEnum activityType)
    {
        return activityType == ActivityTypeEnum.TaskAdded || activityType == ActivityTypeEnum.TaskCompleted || 
               activityType == ActivityTypeEnum.TaskPendingApproval || activityType == ActivityTypeEnum.TaskRejected;
    }
    
    private bool IsApproval(ActivityTypeEnum activityType)
    {
        return activityType == ActivityTypeEnum.ApprovalApproved || activityType == ActivityTypeEnum.ApprovalRejected;
    }

    private string GenerateTitle()
    {
        if (IsTask(Type))
            return Task?.Title ?? "Task Is Null";

        if (IsApproval(Type))
            return Approval?.Status.ToString() ?? "Approval Is Null";

        return "Error Generating Title";
    }
    
    private string GenerateDescription()
    {
        if (IsTask(Type))
            return Task?.Description ?? "Task Is Null";

        if (IsApproval(Type))
            return Approval?.Description.ToString() ?? "Approval Is Null";

        return "Error Generating Title";
    }
    
}