using HR_System.Core.Enums;

namespace HR_System.Core.DTO.Auth;

public class UserDTO
{
    public required string UserName { get; set; }
    public required string Position { get; set; }
    public required string UserId { get; set; }
    public required string UserImageUrl { get; set; }
}