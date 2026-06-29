using System.ComponentModel.DataAnnotations;

namespace HR_System.Core.DTO.Message;

public class MessageAddDTO
{
    [Required]
    public required string Content { get; set; }
    
    [Required]
    public required Guid ReceiverId { get; set; }
    
    
    // override

    public override string ToString()
    {
        return $"Context: {Content}\nReceiverId{ReceiverId}\n";
    }
}