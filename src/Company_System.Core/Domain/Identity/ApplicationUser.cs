using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;
using HR_System.Core.Domain.Entities;
using HR_System.Core.DTO.Auth;
using HR_System.Core.Enums;
using Microsoft.AspNetCore.Identity;

namespace HR_System.Core.Domain.Identity;

public class ApplicationUser : IdentityUser<Guid>
{
    public override Guid Id { get; set; } = Guid.NewGuid();

    public DateTime? CreatedAt { get; set; } = DateTime.UtcNow;

    [Required]
    [Column(TypeName = "nvarchar(100)")]
    public required string FullName { get; set; }

    [Required]
    public required PositionsEnum Position { get; set; }

    [Range(typeof(decimal), "0", "9999999999999999.99")]
    [Column(TypeName = "decimal(18,2)")]
    public decimal? HourlyRate { get; set; }

    [Range(0, 366)]
    public int? AnnualHolidayAllowance { get; set; }


    public UserDTO ToUserDTO()
    {
        return new UserDTO()
        {
            FullName = FullName,
            Email = Email,
            Phone = PhoneNumber,
            HourlyRate = HourlyRate,
            AnnualHolidayAllowance = AnnualHolidayAllowance,
            IsActive = OrganizationHierarchy?.IsActive ?? false,
            Position = Position.ToString(),
            UserId = Id.ToString(),
            UserName = UserName ?? "unknown",
            UserImageUrl = Image?.Url ?? "unknown",
        };
    }

    // relations
    [JsonIgnore]
    public Image? Image { get; set; }

    [JsonIgnore]
    public List<RefreshToken> RefreshTokens { get; set; } = [];

    [JsonIgnore]
    public List<AppTask> Tasks { get; set; } = [];

    [JsonIgnore]
    public List<DailyWorkRecord> DailyWorkRecords { get; set; } = [];

    [JsonIgnore]
    public List<Payment> Payments { get; set; } = [];

    [JsonIgnore]
    public List<Notification> Notifications { get; set; } = [];

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

    [JsonIgnore]
    public List<Message> SentMessages { get; set; } = [];

    [JsonIgnore]
    public List<Message> ReceivedMessages { get; set; } = [];



    // override
    public override string ToString()
    {
        return $"Id: {this.Id}\nEmail: {this.Email}\nUserName: {this.UserName}\nPhoneNumber: {this.PhoneNumber}";
    }

    public override bool Equals(object? obj)
    {
        if (obj is not ApplicationUser otherUser)
        {
            return false;
        }

        return (this.Id == otherUser.Id) &&
               (this.UserName == otherUser.UserName) &&
               (this.Email == otherUser.Email) &&
               (this.PhoneNumber == otherUser.PhoneNumber);
    }
}