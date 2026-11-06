namespace HR_System.Core.DTO.Auth;

public class UserDTO
{
    public required string UserId { get; set; }

    public required string UserName { get; set; }

    public required string FullName { get; set; }

    public required string? Email { get; set; }

    public required string? Phone { get; set; }

    public required string Position { get; set; }

    public required string UserImageUrl { get; set; }

    public decimal? HourlyRate { get; set; }

    public int? AnnualHolidayAllowance { get; set; }

    public required bool IsActive { get; set; }
}
