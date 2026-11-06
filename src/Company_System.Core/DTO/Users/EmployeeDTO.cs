using HR_System.Core.Enums;

namespace HR_System.Core.DTO.Users;

public class EmployeeDTO
{
    public required Guid Id { get; set; }

    public Guid? TreeId { get; set; }

    public Guid? ParentId { get; set; }

    public required string FullName { get; set; }

    public required string? UserName { get; set; }

    public required string? Email { get; set; }

    public required string? Phone { get; set; }

    public required string? ImageUrl { get; set; }

    public required PositionsEnum Position { get; set; }

    public decimal? HourlyRate { get; set; }

    public int? AnnualHolidayAllowance { get; set; }

    public required bool IsActive { get; set; }
}
