using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using HR_System.Core.Domain.Identity;
using HR_System.Core.Enums;

namespace HR_System.Core.DTO.OrganizationHierarchy;

public class OrganizationHierarchyAddDTO
{
    [Required]
    public required Guid Id { get; set; } = Guid.NewGuid();
    
    [Required]
    public required PositionsEnum Position { get; set; }
    
    [Required]
    public required Guid UserId { get; set; }
    
    public required Guid ParentId { get; set; }
}