using HR_System.Core.Enums;

namespace HR_System.Core.DTO.Approval;

public class ToApproveDTO
{
    public required Guid Id { get; set; }
    public required DateTime CreatedOn { get; set; }
    public required string RequesterName { get; set; }
    public required string Body { get; set; }
    
    // override
    override public string ToString()
    {
        return
            $"Id: {Id}\nCreatedOn: {CreatedOn}\n" +
            $"RequesterName: {RequesterName}\nBody: {Body}\n";
    }
}