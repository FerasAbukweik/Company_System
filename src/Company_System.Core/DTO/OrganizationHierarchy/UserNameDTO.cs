namespace HR_System.Core.DTO.OrganizationHierarchy;

public class UserNameDTO
{
    public required string UserName { get; set; }
    public required Guid TreeId { get; set; }
}