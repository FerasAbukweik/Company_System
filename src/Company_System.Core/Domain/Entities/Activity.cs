using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using HR_System.Core.Domain.Identity;
using HR_System.Core.DTO.Activity;
using HR_System.Core.Enums;
using HR_System.Core.ValidationAttributes;
using HR_System.Core.helpers;

namespace HR_System.Core.Domain.Entities;

public class Activity
{
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    public required ActivityTypeEnum Type { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;


    [Column(TypeName = "nvarchar(50)")]
    public string? Title { get; set; }

    [Column(TypeName = "nvarchar(250)")]
    public string? Description { get; set; }

    public Guid? TaskId { get; set; }

    public AppTask? Task { get; set; }

    public Guid? ApprovalId { get; set; }

    public Approval? Approval { get; set; }


    // relations
    [Required]
    public required Guid TriggeredById { get; set; }
    public ApplicationUser? TriggeredBy { get; set; }


    // functions
    private string GetTaskDescription()
    {
        var actor = TriggeredBy?.UserName ?? "Unknown";
        return Type == ActivityTypeEnum.TaskAdded ? $"Priority: {Task!.Priority}\nAdded By {actor}" : $"New Status: {Type switch { ActivityTypeEnum.TaskCompleted => TaskStatusEnum.Completed, ActivityTypeEnum.TaskRejected => TaskStatusEnum.Rejected, _ => TaskStatusEnum.Pending }}\nChanged by: {actor}";
    }


    private string GetApprovalDescription()
    {
        // Event type preserves the status at the time of the event, even after later changes.
        var status = Type switch
        {
            ActivityTypeEnum.ApprovalApproved => ApprovalStatusEnum.Approved,
            ActivityTypeEnum.ApprovalRejected => ApprovalStatusEnum.Rejected,
            _ => ApprovalStatusEnum.Pending
        };
        return ActivityTextGenerator.GetApprovalDescription(Approval!, status);
    }


    public ActivityDto ToDTO()
    {
        return new ActivityDto()
        {
            Id = Id,
            CreatedAt = CreatedAt,
            Type = Type,
            Title = Task != null ? ActivityTextGenerator.GetTaskTitle(Task) : Approval != null ? ActivityTextGenerator.GetApprovalTitle(Approval) : Title ?? "Activity",
            Description = Task != null ? GetTaskDescription() : Approval != null ? GetApprovalDescription() : Description ?? "",
        };
    }
}