using HR_System.Core.common;
using HR_System.Core.DTO.Activity;
using HR_System.Core.DTO.LazyLoading;
using HR_System.Core.Interfaces.ServiceContracts;
using HR_System.ExtensionMethods;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HR_System.Controllers;

public class ActivitiesController(IActivitiesService activityService) : ApplicationControllerBase
{
    [HttpGet]
    [Authorize]
    public async Task<ActionResult<IReadOnlyList<ActivityDTO>>> LazyGet([FromQuery] LazyDTO lazyData, CancellationToken cancellationToken = default)
    {
        var getCurrUserId = User.GetUserId();
        if (!getCurrUserId.IsSuccess) return ((Result)getCurrUserId).ToActionResult(); 
        
        var result = await activityService.LazyGetAllSortedAsync(lazyData, getCurrUserId.Value, cancellationToken);
        return result.ToActionResult();
    }
}