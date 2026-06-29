using HR_System.Core.common;
using HR_System.Core.DTO.OrganizationHierarchy;
using HR_System.Core.Enums;
using HR_System.Core.Interfaces.ServiceContracts.IOrganizationHierarchyService;
using HR_System.ExtensionMethods;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HR_System.Controllers;

public class OrganizationHierarchyController(IOrganizationHierarchyService hierarchyService,
    ILogger<OrganizationHierarchyController> logger) : ApplicationControllerBase
{
    [HttpGet]
    [Authorize]
    public async Task<ActionResult<IReadOnlyList<OrganizationHierarchyDTO>>> GetChildren([FromQuery]IReadOnlyList<Guid>? parents, CancellationToken cancellationToken = default)
    {
        var currUserIdResult = User.GetUserId();
        if (!currUserIdResult.IsSuccess) return ((Result)currUserIdResult).ToActionResult(logger);
        
        
        var result = await hierarchyService.GetChildrenAsync(currUserIdResult.Value, parents, cancellationToken);
        return result.ToActionResult(logger);
    }

    [HttpPost("[action]")]
    [Authorize(Roles = nameof(RolesEnum.Admin))]
    public async Task<IActionResult> Add(OrganizationHierarchyAddDTO toAdd, CancellationToken cancellationToken = default)
    {
        var currUserIdResult = User.GetUserId();
        if (!currUserIdResult.IsSuccess) return ((Result)currUserIdResult).ToActionResult(logger);

        Result result = await hierarchyService.AddAsync(toAdd, currUserIdResult.Value, cancellationToken);
        return result.ToActionResult(logger);
    }

    [HttpDelete("[action]")]
    [Authorize(Roles = nameof(RolesEnum.Admin))]
    public async Task<IActionResult> Delete(Guid toDelete, CancellationToken cancellationToken = default)
    {
        var currUserIdResult = User.GetUserId();
        if (!currUserIdResult.IsSuccess) return ((Result)currUserIdResult).ToActionResult(logger);

        Result result = await hierarchyService.RemoveAsync(toDelete, currUserIdResult.Value, cancellationToken);
        return result.ToActionResult(logger);
    }
}