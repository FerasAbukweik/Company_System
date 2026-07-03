using System.Net;
using System.Security.Claims;
using HR_System.Core.common;
using HR_System.Core.Enums;
using Microsoft.IdentityModel.JsonWebTokens;

namespace HR_System.ExtensionMethods;

public static class ClaimsExtensionMethods
{
    // get user id
    public static Result<Guid> GetUserId(this ClaimsPrincipal user)
    {
        var userIdString = user.FindFirstValue(ClaimTypes.NameIdentifier);
        
        if(string.IsNullOrWhiteSpace(userIdString))
            return Result<Guid>.Failure("userId not found", HttpStatusCode.BadRequest);
        
        if(Guid.TryParse(userIdString, out var userId))
            return Result<Guid>.Success(userId);
        
        return  Result<Guid>.Failure("bad userId formate", HttpStatusCode.BadRequest);
    }
}