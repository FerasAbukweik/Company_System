using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;
using HR_System.Core.Domain.Entities;
using Microsoft.AspNetCore.Identity;

namespace HR_System.Core.Domain.Identity;

public class ApplicationUser : IdentityUser<Guid>
{
    public override Guid Id { get; set; } =  Guid.NewGuid();
    
    [Required]
    [Column(TypeName =  "nvarchar(100)")]
    public required string FullName { get; set; }

    
    // relations
    [JsonIgnore]
    public List<RefreshToken> RefreshTokens { get; set; } = [];
    
    [JsonIgnore]
    public List<AppTask> Tasks { get; set; } = [];
    
    [JsonIgnore]
    public List<AppTask> CreatedTasks { get; set; } = [];
    
    [JsonIgnore]
    public List<Approval> Approvals { get; set; } = [];

    [JsonIgnore]
    public List<Approval> ToApprove { get; set; } = [];
    
    [JsonIgnore]
    public List<Activity> Activities { get; set; } = [];
    
    [JsonIgnore]
    public OrganizationHierarchy OrganizationHierarchy { get; set; }
    
    
    
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