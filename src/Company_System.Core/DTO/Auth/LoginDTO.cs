using System.ComponentModel.DataAnnotations;

namespace HR_System.Core.DTO;

public class LoginDTO
{
    [Required]
    public required string Email { get; set; }
    
    [Required]
    public required  string Password { get; set; }
}