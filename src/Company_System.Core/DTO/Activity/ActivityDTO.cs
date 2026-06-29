using HR_System.Core.Enums;

namespace HR_System.Core.DTO.Activity;

public class ActivityDTO
{
    public required Guid Id { get; set; }
    public required ActivityTypeEnum Type { get; set; }
    public required DateTime CreatedAt { get; set; }
    public required string Title { get; set; }
    public required string Description { get; set; }
    public string? Name { get; set; }
    
    
    
    // override

    public override string ToString()
    {
        return
            $"Id: {Id}\nType: {Type.ToString()}\nCreatedAt: {CreatedAt}\nTitle: {Title}\nDescription: {Description}\nName: {Name}\n";
    }
}