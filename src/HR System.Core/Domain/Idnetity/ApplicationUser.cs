using HR_System.Core.Domain.Entities;
using Microsoft.AspNetCore.Identity;

namespace HR_System.Core.Domain.Idnetity;

public class ApplicationUser : IdentityUser<Guid>
{
    public List<RefreshToken> RefreshTokens { get; set; } = new List<RefreshToken>();
}