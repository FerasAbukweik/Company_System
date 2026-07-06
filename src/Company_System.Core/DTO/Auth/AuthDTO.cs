using HR_System.Core.Enums;

namespace HR_System.Core.DTO.Auth;

public class AuthDTO
{
    public required string UserName { get; set; }
    public required string Role { get; set; }
}