using HR_System.Core.common;
using HR_System.Core.DTO;
using HR_System.Core.ENUM;
using HR_System.Core.Interfaces.ServiceContracts.IAccountServices;
using HR_System.ExtensionMethods;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HR_System.Controllers;

public class Auth(IAccountService accountService) : ApplicationControllerBase
{
    [HttpPost("[action]")]
    [Authorize]
    public IActionResult IsAuthenticated()
    {
        return Ok();
    }
    
    [HttpPost("[action]")]
    [Authorize(Roles =  nameof(RolesEnum.Admin))]
    public IActionResult IsAdmin()
    {
        return Ok();
    }

    [AllowAnonymous]
    [HttpPost("[action]")]
    public async Task<IActionResult> Signup(CreateAccountDTO toCreate, CancellationToken cancellationToken = default)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState.GetErrorsString());
        
        Result result = await accountService.CreateAccountAsync(toCreate, cancellationToken);

        return result.ToActionResult();
    }

    [AllowAnonymous]
    [HttpPost("[action]")]
    public async Task<IActionResult> Login(LoginDTO loginData, CancellationToken cancellationToken = default)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState.GetErrorsString());

        Result result = await accountService.LoginAsync(loginData, cancellationToken);

        return result.ToActionResult();
    }
}