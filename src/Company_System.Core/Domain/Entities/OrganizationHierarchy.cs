using System.ComponentModel.DataAnnotations;
using HR_System.Core.Domain.Identity;
using HR_System.Core.DTO.OrganizationHierarchy;

namespace HR_System.Core.Domain.Entities;

public class OrganizationHierarchy
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public bool IsActive { get; set; } = true;


    // relations
    [Required]
    public required Guid UserId { get; set; }
    public ApplicationUser? User { get; set; }

    public Guid? ParentId { get; set; }
    public OrganizationHierarchy? Parent { get; set; }

    public List<OrganizationHierarchy> Children { get; set; } = [];


    // methods
    public OrganizationHierarchyDTO ToDTO()
    {
        return new OrganizationHierarchyDTO()
        {
            Id = Id,
            UserId = UserId,
            Children = Children.Select(c => c.ToDTO()).ToList(),
            UserName = User?.UserName ?? "unknown",
            FullName = User?.FullName ?? "unknown",
            Position = User?.Position,
            UserImageUrl = User?.Image?.Url ?? "Missing Photo",
        };
    }


    // override
    public override string ToString()
    {
        return $"Id {Id}\nUserId: {UserId}\nParentId: {ParentId}\n";
    }

    public override bool Equals(object? obj)
    {
        if (obj is not OrganizationHierarchy other)
            return false;

        return other.Id == Id &&
               other.UserId == UserId &&
               other.ParentId == ParentId;
    }
}