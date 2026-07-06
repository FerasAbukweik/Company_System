using System.ComponentModel.DataAnnotations;
using HR_System.Core.Domain.Identity;
using HR_System.Core.DTO.OrganizationHierarchy;
using HR_System.Core.Enums;

namespace HR_System.Core.Domain.Entities;

public class OrganizationHierarchy
{
    public Guid Id { get; set; } = Guid.NewGuid();
    
    [Required]
    public required PositionsEnum Position { get; set; }
    
    
    
    // relations
    
    [Required]
    public required Guid UserId { get; set; }
    public ApplicationUser? User { get; set; }
    
    public Guid? ParentId { get; set; }
    public OrganizationHierarchy? Parent { get; set; }

    public List<OrganizationHierarchy> Children { get; set; } = [];
    
    
    
    // functions

    public OrganizationHierarchyDTO ToDTO(Guid currUserId)
    {
        return new OrganizationHierarchyDTO()
        {
            Id = Id,
            UserId = UserId,
            Position = Position,
            Children = Children.Select(c => c.ToDTO(currUserId)).ToList(),
            IsCurrUser = UserId == currUserId
        };
    }
    
    
    
    // override

    public override string ToString()
    {
        return $"Id {Id}\n Position: {Position.ToString()}\nUserId: {UserId}\nParentId: {ParentId}\n";
    }

    public override bool Equals(object? obj)
    {
        if(obj is not OrganizationHierarchy other)
            return false;
        
        return other.Id == Id &&
               other.Position == Position &&
               other.UserId == UserId &&
               other.ParentId == ParentId;
    }
}