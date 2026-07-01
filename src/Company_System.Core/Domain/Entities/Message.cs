using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;
using HR_System.Core.Domain.Identity;
using HR_System.Core.DTO.Message;

namespace HR_System.Core.Domain.Entities;

public class Message
{
    public Guid Id { get; set; } =  Guid.NewGuid();
    
    [Required]
    [Column(TypeName = "nvarchar(500)")]
    public required string Content { get; set; }
    
    public DateTime CreatedAt { get; set; } = DateTime.Now;
    
    
    
    // relations
    
    [Required]
    public required Guid SenderId { get; set; }
    
    [JsonIgnore]
    public ApplicationUser? Sender { get; set; }
    
    
    [Required]
    public required Guid ReceiverId { get; set; }
    
    [JsonIgnore]
    public ApplicationUser? Receiver { get; set; }
    
    
    // functions

    public MessageDTO ToDTO(Guid currUserId)
    {
        return new MessageDTO()
        {
            Id = Id,
            Content = Content,
            CreatedAt = CreatedAt,
            IsCurrUserSender = SenderId == currUserId
        };
    }
    
    
    
    // override

    public override string ToString()
    {
        return $"Id: {Id}\nContent: {Content}\nCreatedAt: {CreatedAt}\nSenderId: {SenderId}\nReceiverId: {ReceiverId}";
    }

    public override bool Equals(object? obj)
    {
        if (obj is not Message other)
            return false;
        
        return other.Id == Id && other.Content == Content &&
               other.CreatedAt == CreatedAt && other.SenderId == SenderId &&
               other.ReceiverId == ReceiverId;
    }
}