using HR_System.Core.common;
using HR_System.Core.DTO.Account;
using HR_System.Core.Enums;
using HR_System.Core.Interfaces.ServiceContracts;
using HR_System.ExtensionMethods;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HR_System.Controllers;

public class AccountController(
    IAccountOrgHierarchyService accountOrgHierarchyService
    ) : ApplicationControllerBase
{
    [HttpPost("[action]")]
    [Authorize(Roles = nameof(RolesEnum.Admin))]
    [Transactional]
    public async Task<IActionResult> AddEmployee([FromForm]AddEmployeeDTO toCreate,
        CancellationToken cancellationToken = default)
    {
        Result result = await accountOrgHierarchyService.AddEmployee(toCreate, cancellationToken);

        return result.ToActionResult();
    }
}