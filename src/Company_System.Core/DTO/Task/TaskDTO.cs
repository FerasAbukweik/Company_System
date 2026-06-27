using HR_System.Core.Domain.Idnetity;
using HR_System.Core.ENUM;
using HR_System.Core.Enums;

namespace HR_System.Core.DTO.Task;

public class TaskDTO
{
    public required Guid Id { get; set; }
    public required string Title { get; set; }
    public required string Description { get; set; }
    public required DateTime Created { get; set; }
    public required DateTime Deadline { get; set; }
    public required PrioritiesEnum Priority { get; set; }
    public required TaskStatusEnum Status { get; set; }
    public required Guid UserId { get; set; }
    public required Guid ManagerId { get; set; }
}