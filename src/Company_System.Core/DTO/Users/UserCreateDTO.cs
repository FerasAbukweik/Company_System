using System.ComponentModel.DataAnnotations;
using HR_System.Core.Enums;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace HR_System.Core.DTO.Users;

public class UserCreateDTO
{
    [BindNever]
    public Guid UserId { get; set; } = Guid.NewGuid();

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
    [EnumDataType(typeof(PositionsEnum))]
    public required PositionsEnum Position { get; set; }


    [Range(typeof(decimal), "0", "9999999999999999.99")]
    public decimal? HourlyRate { get; set; }

    [Range(0, 366)]
    public int? AnnualHolidayAllowance { get; set; }


    // override

    public override string ToString()
    {
        return
            $"Email: {Email}\nPassword: {Password}\nUserName: {UserName}\nFullName: {FullName}\nPhoneNumber: {PhoneNumber}";
    }
}