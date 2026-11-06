using System.ComponentModel.DataAnnotations;
using HR_System.Core.DTO.LazyLoading;

namespace HR_System.Core.DTO.Users;

public class EmployeeQueryDTO : LazyDTO
{
    public bool ActiveOnly { get; set; }

    [MaxLength(100)]
    public string? Search { get; set; }
}
