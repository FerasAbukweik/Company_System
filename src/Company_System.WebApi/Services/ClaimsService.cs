using System.Security.Claims;
using HR_System.Core.Interfaces.ServiceContracts;

namespace HR_System.Services;

public class ClaimsService(IHttpContextAccessor contextAccessor) : IClaimsService
{
    public string GetUserName()
    {
        return contextAccessor?.HttpContext?.User.FindFirstValue(ClaimTypes.Name) ?? "unknown";
    }
}