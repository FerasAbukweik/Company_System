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
    ITasksApprovalsService tasksApprovalsService) : ApplicationControllerBase
{
    [HttpGet("[action]")]
    [Authorize]
    public async Task<ActionResult<IReadOnlyList<ToApproveDTO>>> GetNeedsApproval([FromQuery]LazyDTO lazyData, CancellationToken cancellationToken = default)
    {
        var userIdResult = User.GetUserId();
        if (!userIdResult.IsSuccess) return ((Result)userIdResult).ToActionResult();

        var result = await approvalService.GetNeedsApprovalAsync(lazyData, userIdResult.Value, cancellationToken);

        return result.ToActionResult();
    }
    
    [HttpGet("[action]")]
    [Authorize]
    public async Task<ActionResult<IReadOnlyList<RequestedApprovalDTO>>> GetRequested([FromQuery]LazyDTO lazyData, CancellationToken cancellationToken = default)
    {
        var userIdResult = User.GetUserId();
        if (!userIdResult.IsSuccess) return ((Result)userIdResult).ToActionResult();

        var result = await approvalService.GetRequested(lazyData, userIdResult.Value, cancellationToken);

        return result.ToActionResult();
    }

    [HttpPut("[action]/{approvalId:guid}")]
    [Authorize]
    [Transactional]
    public async Task<IActionResult> UpdateStatus([FromRoute]Guid approvalId, [FromQuery] ApprovalStatusEnum newStatus, CancellationToken cancellationToken = default)
    {
        var userIdResult = User.GetUserId();
        if (!userIdResult.IsSuccess) return ((Result)userIdResult).ToActionResult();

        Result result =
            await tasksApprovalsService.UpdateApprovalStatus(approvalId, newStatus, userIdResult.Value, cancellationToken);
        
        return result.ToActionResult();
    }

    [HttpPost("[action]")]
    [Authorize]
    [Transactional]
    public async Task<IActionResult> RequestHoliday(CancellationToken cancellationToken = default)
    {
        var userIdResult = User.GetUserId();
        if (!userIdResult.IsSuccess) return ((Result)userIdResult).ToActionResult();
        
        Result result = await approvalService.RequestHoliday(userIdResult.Value, cancellationToken);
        
        return result.ToActionResult();
    }
    
}