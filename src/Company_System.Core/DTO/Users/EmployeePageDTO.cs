namespace HR_System.Core.DTO.Users;

public class EmployeePageDTO
{
    public required IReadOnlyList<EmployeeDTO> Items { get; set; }

    public required int TotalCount { get; set; }

}
