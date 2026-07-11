using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace HR_System.Core.DTO.Account;

public class AddEmployeeDTO : UserCreateDTO
{
    
    [Required]
    public required Guid ParentId { get; set; }
    
    [Required]
    public required IFormFile Image { get; set; }
    
    
    // override

    public override string ToString()
    {
        return
            $"Email: {Email}\nPassword: {Password}\nUserName: {UserName}\nFullName: {FullName}\nPhoneNumber: {PhoneNumber}\nParentId: {ParentId}";
    }
}