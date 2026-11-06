using HR_System.Core.Abstraction.ServiceContracts;
using HR_System.Core.DTO.Auth;
using HR_System.Core.Enums;
using HR_System.Core.Result;
using HR_System.ExtensionMethods;
using HR_System.Filters;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HR_System.Controllers;

public class AuthController(
    IAuthService authService) : ApplicationControllerBase
{
    [HttpPost("[action]")]
    [Authorize(Roles = nameof(RolesEnum.Admin))]
    public ActionResult<UserDTO> IsAdmin()
    {
        return Ok();
    }

    [HttpPost("[action]")]
    [AllowAnonymous]
    public IActionResult Logout()
    {
        Result result = authService.Logout();

        return result.ToActionResult();
    }

    [AllowAnonymous]
    [HttpPost("[action]")]
    public async Task<ActionResult<UserDTO>> Login(LoginDTO loginData, CancellationToken cancellationToken = default)
    {
        var result = await authService.LoginAsync(loginData, cancellationToken);

        return result.ToActionResult();
    }

    [AllowAnonymous]
    [HttpPost("[action]")]
    [Transactional]
    public async Task<IActionResult> UpdateTokens(CancellationToken cancellationToken = default)
    {
        Result result = await authService.RefreshTokensAsync(cancellationToken);

        return result.ToActionResult();
    }
}
