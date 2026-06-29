using HR_System.Core.Enums;

namespace HR_System.Core.DTO.Approval;

public class ApprovalDTO
{
    public required Guid Id { get; set; }
    public required ApprovalStatusEnum Status { get; set; }
    public required ApprovalTypeEnum Type { get; set; }
    public required DateTime CreatedOn { get; set; }
    public Guid? TaskId { get; set; }
    public required Guid UserRequestingId { get; set; }
    public required Guid ManagerId { get; set; }
    
    
    // override
    override public string ToString()
    {
        return
            $"Id: {Id}\nStatus: {Status.ToString()}\nType: {Type.ToString()}\nCreatedOn: {CreatedOn}\n" +
            $"TaskId: {TaskId}\nUserRequestingId: {UserRequestingId}\nManagerId: {ManagerId}\n";
    }
}