namespace HR_System.Core.DTO.Users;

public class EmployeeSummaryDTO
{
    public int Headcount { get; set; }

    public int Active { get; set; }

    public decimal TotalHourlyRates { get; set; }

    public int ConfiguredHourlyRates { get; set; }
}
