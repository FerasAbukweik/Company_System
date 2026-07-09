using HR_System.Core.common;
using HR_System.Core.DTO.LazyLoading;
using HR_System.Core.DTO.Message;
using HR_System.Core.Interfaces.ServiceContracts;
using HR_System.ExtensionMethods;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HR_System.Controllers;

public class MessagesController(IMessagesService messagesService,
    ILogger<MessagesController> logger) : ApplicationControllerBase
{
    [HttpGet]
    [Authorize]
    public async Task<ActionResult<IReadOnlyList<MessageDTO>>> LazyGetMessages([FromQuery]LazyDTO lazyData, CancellationToken cancellationToken = default)
    {
        var currUserIdResult = User.GetUserId();
        if (!currUserIdResult.IsSuccess) return ((Result)currUserIdResult).ToActionResult(logger);
        
        var result = await messagesService.LazyGetMessages(currUserIdResult.Value!, lazyData, cancellationToken);
        return result.ToActionResult(logger);
    }
}