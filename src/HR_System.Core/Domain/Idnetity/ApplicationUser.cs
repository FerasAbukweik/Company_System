using System.ComponentModel.DataAnnotations;
using HR_System.Core.Domain.Entities;
using HR_System.Core.ENUM;
using Microsoft.AspNetCore.Identity;

namespace HR_System.Core.Domain.Idnetity;

public class ApplicationUser : IdentityUser<Guid>
{
    public List<RefreshToken> RefreshTokens { get; set; } = new List<RefreshToken>();
    
    [Required]
    public required string FullName { get; set; }

    // override
    public override string ToString()
    {
        return $"Id: {this.Id}\nEmail: {this.Email}\nUserName: {this.UserName}\nPhoneNumber: {this.PhoneNumber}";
    }

    public override bool Equals(object? obj)
    {
        if (obj is not ApplicationUser otherUser)
        {
            return  false;
        }
        
        return (this.Id == otherUser.Id) &&
               (this.UserName == otherUser.UserName) &&
               (this.Email == otherUser.Email) &&
               (this.PhoneNumber == otherUser.PhoneNumber);
    }
}