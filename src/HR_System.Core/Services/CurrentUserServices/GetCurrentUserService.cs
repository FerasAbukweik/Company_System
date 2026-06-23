using System.Net;
using System.Security.Claims;
using HR_System.Core.Domain.Idnetity;
using HR_System.Core.Helpers;
using HR_System.Core.ServiceContracts.ICurrentUserServices;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;

namespace HR_System.Core.Services.CurrentUserServices;

public class GetCurrentUserService : IGetCurrentUserService
{
    private readonly IHttpContextAccessor _accessor;
    private readonly UserManager<ApplicationUser> _userManager;

    public GetCurrentUserService(IHttpContextAccessor accessor,
        UserManager<ApplicationUser> userManager)
    {
        _accessor = accessor;
        _userManager = userManager;
    }
    
    public Result<Guid> GetUserId()
    {
        var userIdString = _accessor.HttpContext?.User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (string.IsNullOrWhiteSpace(userIdString))
        {
            return Result<Guid>.Failure("userIdString is Null");
        }

        if (!Guid.TryParse(userIdString, out var userIdGuid))
        {
            return Result<Guid>.Failure("failed to parse userIdString to Guid");
        }
        
        return Result<Guid>.Success(userIdGuid);
    }
    public async Task<Result<ApplicationUser>> GetCurrUserObjectAsync()
    {
        var getUserIdResult = this.GetUserId();
        if (!getUserIdResult.IsSuccess) return getUserIdResult.MapFailure<ApplicationUser>();

        ApplicationUser? currentUser = await _userManager.FindByIdAsync(getUserIdResult.Value.ToString());

        if (currentUser is null)
        {
            return Result<ApplicationUser>.Failure("User not found" , HttpStatusCode.NotFound);
        }
        
        return Result<ApplicationUser>.Success(currentUser);
    }
}