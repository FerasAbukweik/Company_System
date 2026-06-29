using System.ComponentModel.DataAnnotations;

namespace HR_System.Core.DTO.Auth;

public class LoginDTO
{
    [Required]
    public required string Email { get; set; }
    
    [Required]
    public required  string Password { get; set; }
    
    // override

    override public string ToString()
    {
        return $"Email: {Email}\nPassword: {Password}\n";
    }
}