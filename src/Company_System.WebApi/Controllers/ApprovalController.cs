using HR_System.Core.common;
using HR_System.Core.DTO.Approval;
using HR_System.Core.DTO.LazyLoading;
using HR_System.Core.Enums;
using HR_System.Core.Interfaces.ServiceContracts;
using HR_System.ExtensionMethods;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HR_System.Controllers;

public class ApprovalController(IApprovalService approvalService,
    ILogger<ApprovalController> logger) : ApplicationControllerBase
{
    [HttpGet("[action]")]
    [Authorize]
    public async Task<ActionResult<IReadOnlyList<ToApproveDTO>>> GetNeedsApproval([FromQuery]LazyDTO lazyData, CancellationToken cancellationToken = default)
    {
        var userIdResult = User.GetUserId();
        if (!userIdResult.IsSuccess) return ((Result)userIdResult).ToActionResult(logger);

        var result = await approvalService.GetNeedsApprovalAsync(lazyData, userIdResult.Value, cancellationToken);

        return result.ToActionResult(logger);
    }
    
    [HttpGet("[action]")]
    [Authorize]
    public async Task<ActionResult<IReadOnlyList<RequestedApproval>>> GetRequested([FromQuery]LazyDTO lazyData, CancellationToken cancellationToken = default)
    {
        var userIdResult = User.GetUserId();
        if (!userIdResult.IsSuccess) return ((Result)userIdResult).ToActionResult(logger);

        var result = await approvalService.GetRequested(lazyData, userIdResult.Value, cancellationToken);

        return result.ToActionResult(logger);
    }

    [HttpPut("[action]/{approvalId:guid}")]
    [Authorize]
    public async Task<IActionResult> UpdateStatus([FromRoute]Guid approvalId, [FromQuery] ApprovalStatusEnum newStatus, CancellationToken cancellationToken = default)
    {
        var userIdResult = User.GetUserId();
        if (!userIdResult.IsSuccess) return ((Result)userIdResult).ToActionResult(logger);

        Result result =
            await approvalService.UpdateStatus(approvalId, newStatus, userIdResult.Value, cancellationToken);
        
        return result.ToActionResult(logger);
    }

    [HttpPost("[action]")]
    [Authorize]
    public async Task<IActionResult> Add([FromBody] ApprovalAddDTO toAddData, CancellationToken cancellationToken = default)
    {
        var userIdResult = User.GetUserId();
        if (!userIdResult.IsSuccess) return ((Result)userIdResult).ToActionResult(logger);
        
        Result result = await approvalService.AddAsync(toAddData, userIdResult.Value, cancellationToken);
        
        return result.ToActionResult(logger);
    }
    
}