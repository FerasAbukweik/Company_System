using System.Security.Claims;
using HR_System.Core.common;
using HR_System.Core.DTO.Auth;
using HR_System.Core.Enums;
using HR_System.Core.Interfaces.ServiceContracts;
using HR_System.ExtensionMethods;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HR_System.Controllers;

public class AuthController(IAccountService accountService,
    ILogger<AuthController> logger,
    ITokenService tokenService,
    ICookiesServices cookiesServices) : ApplicationControllerBase
{
    [HttpPost("[action]")]
    [Authorize]
    public ActionResult<AuthDTO> IsAuthenticated()
    {
        var role = User.FindFirstValue(ClaimTypes.Role);
        return Ok(new AuthDTO()
        {
            UserName = User.Identity?.Name ?? "Identity is null",
            Role = role ?? "role is null"
        });
    }
    
    [HttpPost("[action]")]
    [Authorize(Roles = nameof(RolesEnum.Admin))]
    public ActionResult<AuthDTO> IsAdmin()
    {
        return Ok();
    }

    [HttpPost("[action]")]
    [Authorize]
    public IActionResult Logout()
    {
        cookiesServices.RemoveTokens();

        return Ok();
    }

    [AllowAnonymous] // only for testing
    [HttpPost("[action]")]
    public async Task<IActionResult> Signup(AccountCreateDTO toAccountCreate, CancellationToken cancellationToken = default)
    {
        Result result = await accountService.CreateAccountAsync(toAccountCreate, cancellationToken);

        return result.ToActionResult(logger);
    }

    [AllowAnonymous]
    [HttpPost("[action]")]
    public async Task<IActionResult> Login(LoginDTO loginData, CancellationToken cancellationToken = default)
    {
        Result result = await accountService.LoginAsync(loginData, cancellationToken);

        return result.ToActionResult(logger);
    }

    [AllowAnonymous]
    [HttpPost("[action]")]
    public async Task<IActionResult> UpdateTokens(CancellationToken cancellationToken = default)
    {
        Result result = await tokenService.UpdateUserTokensAsync(cancellationToken);

        return result.ToActionResult(logger);
    }
}