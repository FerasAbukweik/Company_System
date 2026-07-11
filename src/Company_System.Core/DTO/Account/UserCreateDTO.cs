using System.ComponentModel.DataAnnotations;
using HR_System.Core.Enums;

namespace HR_System.Core.DTO.Account;

public class UserCreateDTO
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
    public required PositionsEnum Position { get; set; }
    

    // override

    public override string ToString()
    {
        return
            $"Email: {Email}\nPassword: {Password}\nUserName: {UserName}\nFullName: {FullName}\nPhoneNumber: {PhoneNumber}";
    }
}