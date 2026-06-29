namespace HR_System.Core.DTO.Message;

public class MessageDTO
{
    public required Guid Id { get; set; }
    public required string Content { get; set; }
    public required DateTime CreatedAt { get; set; }
    public required bool IsCurrUserSender { get; set; }
    
    
    // override

    public override string ToString()
    {
        return $"Id: {Id}\nContent: {Content}\nCreatedAt: {CreatedAt}\nIsCurrUserSender: {IsCurrUserSender}\n";
    }
}