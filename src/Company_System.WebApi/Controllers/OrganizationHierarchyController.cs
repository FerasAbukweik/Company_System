using HR_System.Core.common;
using HR_System.Core.DTO.LazyLoading;
using HR_System.Core.DTO.OrganizationHierarchy;
using HR_System.Core.Enums;
using HR_System.Core.Interfaces.ServiceContracts;
using HR_System.ExtensionMethods;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HR_System.Controllers;

public class OrganizationHierarchyController(
    IOrganizationHierarchyService hierarchyService
    ) : ApplicationControllerBase
{
    [HttpGet]
    [Authorize]
    public async Task<ActionResult<IReadOnlyDictionary<Guid, IReadOnlyList<OrganizationHierarchyDTO>>>> GetChildren([FromQuery]IReadOnlyList<Guid>? parents, CancellationToken cancellationToken = default)
    {
        var result = await hierarchyService.GetChildrenAsync(parents, cancellationToken);
        return result.ToActionResult();
    }

    [HttpGet("[action]")]
    [Authorize(Roles = nameof(RolesEnum.Admin))]
    public async Task<ActionResult<IReadOnlyList<UserNameDTO>>> GetUserNames([FromQuery]LazyDTO lazyData, CancellationToken cancellationToken = default)

    {
        var result = await hierarchyService.GetUserNames(lazyData, cancellationToken);

        return result.ToActionResult();
    }
}