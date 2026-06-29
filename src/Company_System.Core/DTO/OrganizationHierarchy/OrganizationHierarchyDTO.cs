using System.Text;
using HR_System.Core.Enums;

namespace HR_System.Core.DTO.OrganizationHierarchy;

public class OrganizationHierarchyDTO
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public required PositionsEnum Position { get; set; }
    public required Guid UserId { get; set; }
    public required List<OrganizationHierarchyDTO> Children { get; set; }
    public required bool IsCurrUser { get; set; }
    
    
    // override

    override public string ToString()
    {
        var sb = new StringBuilder();
        foreach (var child in Children)
            sb.Append($"Child: {child.ToString()}\n");

        return
            $"Id: {Id}\nPosition: {Position.ToString()}\nUserId: {UserId}\nChildren: {sb.ToString()}\nIsCurrUser: {IsCurrUser}\n";
    }
}