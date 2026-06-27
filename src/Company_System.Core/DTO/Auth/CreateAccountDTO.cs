using System.ComponentModel.DataAnnotations;
using HR_System.Core.ENUM;

namespace HR_System.Core.DTO.Auth;

public class CreateAccountDTO
{
    [Required]
    public required string Email { get; set; }
    [Required]
    public required string Password { get; set; }
    [Required]
    public required string UserName { get; set; }
    [Required]
    public required string FullName { get; set; }
    [Required]
    public required string PhoneNumber { get; set; }
    [Required]
    public required RolesEnum Role { get; set; }
    
    
    // override

    public override string ToString()
    {
        return
            $"Email: {Email}\nPassword: {Password}\nUserName: {UserName}\nFullName: {FullName}\nPhoneNumber: {PhoneNumber}\nRole: {Role.ToString()}";
    }
}