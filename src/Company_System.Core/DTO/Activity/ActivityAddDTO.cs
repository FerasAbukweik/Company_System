using HR_System.Core.Enums;
using HR_System.Core.ValidationAttributes;

namespace HR_System.Core.DTO.Activity;

public class ActivityAddDTO
{
    public required ActivityTypeEnum Type { get; set; }
    public required string Title { get; set; } 
    public required string Description { get; set; }
    
    
    // override

    public override string ToString()
    {
        return $"Type: {Type.ToString()}\nTitle: {Title}\nDescription: {Description}";
    }
}