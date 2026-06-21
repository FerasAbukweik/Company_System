using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using HR_System.Core.Domain.Idnetity;

namespace HR_System.Core.Domain.Entities;

public class RefreshToken
{
    [Key]
    public Guid Id { get; set; }
    
    [Required(ErrorMessage = "{0} Is Required")]
    public required string Token { get; set; }
    
    [Required(ErrorMessage = "{0} Is Required")]
    public required DateTime Expires { get; set; }
    
    
    // foregin relations
    [Required(ErrorMessage = "{0} Is Required")]
    public required Guid UserId { get; set; }
    
    public ApplicationUser? User { get; set; }
}