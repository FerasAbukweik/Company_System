using HR_System.Core.DTO.Auth;
using HR_System.Core.Abstraction.ServiceContracts;
using HR_System.Core.DTO.Users;
using HR_System.Core.DTO.LazyLoading;
using HR_System.Core.Enums;
using HR_System.Core.Result;
using HR_System.ExtensionMethods;
using HR_System.Filters;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HR_System.Controllers;

public class UsersController(
    IUsersService usersService
    ) : ApplicationControllerBase
{
    [HttpPost("[action]")]
    [Authorize(Roles = nameof(RolesEnum.Admin))]
    [Transactional]
    public async Task<IActionResult> AddEmployee([FromForm] AddEmployeeDTO toCreate,
        CancellationToken cancellationToken = default)
    {
        Result result = await usersService.AddEmployeeAsync(toCreate, cancellationToken);

        return result.ToActionResult();
    }


    [HttpGet("[action]")]
    [Authorize(Roles = nameof(RolesEnum.Admin))]
    public async Task<ActionResult<EmployeePageDTO>> GetEmployees(
        [FromQuery] EmployeeQueryDTO lazyData,
        CancellationToken cancellationToken = default)
    {
        var result = await usersService.LazyGetEmployeesAsync(lazyData, cancellationToken);

        return result.ToActionResult();
    }
    [HttpGet("Me")]
    [Authorize]
    public async Task<ActionResult<UserDTO>> Me(CancellationToken cancellationToken = default)
    {
        var userId = User.GetUserId();
        if (!userId.IsSuccess) return ((Result)userId).ToActionResult();
        return (await usersService.GetUserAsync(userId.Value, cancellationToken)).ToActionResult();
    }

    [HttpPut("UpdateEmployee/{userId:guid}")]
    [Authorize(Roles = nameof(RolesEnum.Admin))]
    [Transactional]
    public async Task<ActionResult<EmployeeDTO>> UpdateEmployee(Guid userId, [FromForm] EmployeeUpdateDTO data, CancellationToken cancellationToken = default)
    {
        return (await usersService.UpdateEmployeeAsync(userId, data, cancellationToken)).ToActionResult();
    }

    [HttpPut("Deactivate/{userId:guid}")]
    [Authorize(Roles = nameof(RolesEnum.Admin))]
    [Transactional]
    public async Task<IActionResult> Deactivate(Guid userId, CancellationToken cancellationToken = default)
    {
        var currentUserId = User.GetUserId();
        if (!currentUserId.IsSuccess) return ((Result)currentUserId).ToActionResult();
        return (await usersService.DeactivateEmployeeAsync(userId, currentUserId.Value, cancellationToken)).ToActionResult();
    }
}