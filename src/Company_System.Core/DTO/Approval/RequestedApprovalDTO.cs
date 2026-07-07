using HR_System.Core.Enums;

namespace HR_System.Core.DTO.Approval;

public class RequestedApprovalDTO
{
    public required Guid Id { get; set; }
    public required DateTime CreatedOn { get; set; }
    public required string RequesterName { get; set; }
    public required string Body { get; set; }
    public required ApprovalStatusEnum Status { get; set; }
    
    // override
    override public string ToString()
    {
        return
            $"Id: {Id}\nCreatedOn: {CreatedOn}\nStatus: {Status}" +
            $"RequesterName: {RequesterName}\nBody: {Body}\n";
    }
}