using HR_System.Core.Enums;

namespace HR_System.Core.DTO.Auth;

public class AuthDTO
{
    public required bool IsAuthenticated { get; set; }
    public required RolesEnum[] Roles { get; set; }
    public required DateTime TokenExpiresAt { get; set; }
}