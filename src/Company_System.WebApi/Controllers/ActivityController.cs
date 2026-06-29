using HR_System.Core.common;
using HR_System.Core.DTO.Activity;
using HR_System.Core.DTO.LazyLoading;
using HR_System.Core.Interfaces.ServiceContracts.IActivitiesService;
using HR_System.ExtensionMethods;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HR_System.Controllers;

public class ActivityController(IActivitiesService activityService,
    ILogger<ActivityController> logger) : ApplicationControllerBase
{
    [HttpGet]
    [Authorize]
    public async Task<ActionResult<IReadOnlyList<ActivityDTO>>> GetAll([FromQuery] LazyDTO lazyData, CancellationToken cancellationToken = default)
    {
        var result = await activityService.LazyGetAllSortedAsync(lazyData, cancellationToken);
        return result.ToActionResult(logger);
    }
}