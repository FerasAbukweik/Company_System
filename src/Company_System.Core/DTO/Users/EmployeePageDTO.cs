namespace HR_System.Core.DTO.Users;

public class EmployeePageDTO
{
    public required IReadOnlyList<EmployeeDTO> Items { get; set; }

    public required int TotalCount { get; set; }

    public required EmployeeSummaryDTO Summary { get; set; }
}

public class EmployeeSummaryDTO
{
    public int Headcount { get; set; }

    public int Active { get; set; }

    public decimal TotalHourlyRates { get; set; }

    public int ConfiguredHourlyRates { get; set; }
}
