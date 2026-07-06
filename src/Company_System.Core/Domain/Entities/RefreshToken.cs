using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using HR_System.Core.Domain.Identity;

namespace HR_System.Core.Domain.Entities;

public class RefreshToken
{ 
    public Guid Id { get; set; } = Guid.NewGuid();
    
    [Required]
    [Column(TypeName = "varchar(100)")]
    public required string Token { get; set; }
    
    [Required]
    public required DateTime Expires { get; set; }
    public bool IsResolved => Expires <= DateTime.UtcNow;
    
    
    // relations
    [Required]
    public required Guid UserId { get; set; }
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