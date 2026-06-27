using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;
using HR_System.Core.Domain.Idnetity;

namespace HR_System.Core.Domain.Entities;

public class RefreshToken
{
    [Key] public Guid Id { get; set; } = Guid.NewGuid();
    
    [Required(ErrorMessage = "{0} Is Required")]
    [Column(TypeName = "varchar(max)")]
    public required string Token { get; set; }
    
    [Required(ErrorMessage = "{0} Is Required")]
    public required DateTime Expires { get; set; }
    public bool IsResolved => Expires <= DateTime.UtcNow;
    
    
    // foregin relations
    [Required(ErrorMessage = "{0} Is Required")]
    public required Guid UserId { get; set; }
    
    [JsonIgnore]
    public ApplicationUser? User { get; set; }
    
    
    
    // override
    public override string ToString()
    {
        return $"Id: {this.Id}\nUSerId: {this.UserId}\nExpires: {this.Expires}\nToken: {this.Token}\n";
    }

    public override bool Equals(object? obj)
    {
        if (obj is not RefreshToken otherRefreshToken)
        {
            return false;
        }
        
        return (this.Id == otherRefreshToken.Id) && 
               (this.UserId == otherRefreshToken.UserId) &&
               (this.Expires == otherRefreshToken.Expires) &&
               (this.Token == otherRefreshToken.Token);
    }
}