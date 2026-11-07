using HR_System.Core.Abstraction.ServiceContracts;
using HR_System.Core.DTO.LazyLoading;
using HR_System.Core.DTO.Notification;
using HR_System.Core.Result;
using HR_System.ExtensionMethods;
using HR_System.Filters;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HR_System.Controllers;

[Authorize]
public class NotificationsController(INotificationsService notificationsService) : ApplicationControllerBase
{
    [HttpGet]
    public async Task<ActionResult<NotificationPageDTO>> Get([FromQuery] LazyDTO lazyData, CancellationToken cancellationToken)
    {
        var userId = User.GetUserId();
        if (!userId.IsSuccess) return ((Result)userId).ToActionResult();

        return (await notificationsService.GetAsync(userId.Value, lazyData, cancellationToken)).ToActionResult();
    }

    [HttpGet("[action]")]
    public async Task<ActionResult<int>> GetUnreadCount(CancellationToken cancellationToken)
    {
        var userId = User.GetUserId();
        if (!userId.IsSuccess) return ((Result)userId).ToActionResult();

        return (await notificationsService.GetUnreadCountAsync(userId.Value, cancellationToken)).ToActionResult();
    }

    [HttpPut("[action]")]
    [Transactional]
    public async Task<ActionResult<bool>> MarkAllRead(CancellationToken cancellationToken)
    {
        var userId = User.GetUserId();
        if (!userId.IsSuccess) return ((Result)userId).ToActionResult();

        return (await notificationsService.MarkAllReadAsync(userId.Value, cancellationToken)).ToActionResult();
    }
}
