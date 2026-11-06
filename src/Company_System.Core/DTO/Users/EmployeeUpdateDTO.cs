using System.ComponentModel.DataAnnotations;
using HR_System.Core.Enums;
using Microsoft.AspNetCore.Http;

namespace HR_System.Core.DTO.Users;

public class EmployeeUpdateDTO
{
    [Required, MaxLength(100)]
    public required string FullName { get; set; }

    [Required]
    public required string UserName { get; set; }

    [Required, EmailAddress]
    public required string Email { get; set; }

    [Required]
    public required string PhoneNumber { get; set; }

    [EnumDataType(typeof(PositionsEnum))]
    public required PositionsEnum Position { get; set; }

    [Range(typeof(decimal), "0", "9999999999999999.99")]
    public decimal? HourlyRate { get; set; }

    [Range(0, 366)]
    public int? AnnualHolidayAllowance { get; set; }

    public Guid? ParentId { get; set; }

    public IFormFile? Image { get; set; }
}
