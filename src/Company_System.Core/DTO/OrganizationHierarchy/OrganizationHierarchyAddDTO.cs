using System.ComponentModel.DataAnnotations;
using HR_System.Core.Enums;

namespace HR_System.Core.DTO.OrganizationHierarchy;

public class OrganizationHierarchyAddDTO
{
    public Guid Id { get; set; } = Guid.NewGuid();
    
    [Required]
    public required PositionsEnum Position { get; set; }
    
    [Required]
    public required Guid UserId { get; set; }
    
    public required Guid ParentId { get; set; }
    
    
    // override

    override public string ToString()
    {
        return $"Id: {Id}\nPosition: {Position.ToString()}\nUserId: {UserId}\nParentId: {ParentId}\n";
    }
}