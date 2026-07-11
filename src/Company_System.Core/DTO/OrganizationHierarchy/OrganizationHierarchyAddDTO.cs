using System.ComponentModel.DataAnnotations;
using HR_System.Core.Enums;

namespace HR_System.Core.DTO.OrganizationHierarchy;

public class OrganizationHierarchyAddDTO
{
    [Required]
    public required Guid UserId { get; set; }
    
    [Required]
    public required Guid ParentId { get; set; }
    
    
    // override

    override public string ToString()
    {
        return $"UserId: {UserId}\nParentId: {ParentId}\n";
    }
}