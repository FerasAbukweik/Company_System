using HR_System.Core.common;
using HR_System.Core.DTO.Approval;
using HR_System.Core.Enums;
using HR_System.Core.Interfaces.ServiceContracts.IApprovalService;
using HR_System.ExtensionMethods;
using Microsoft.AspNetCore.Mvc;

namespace HR_System.Controllers;

public class ApprovalController(IApprovalService approvalService) : ApplicationControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<ApprovalDTO>>> GetNeedsApproval(CancellationToken cancellationToken = default)
    {
        var userIdResult = User.GetUserId();
        if (!userIdResult.IsSuccess) return ((Result)userIdResult).ToActionResult();

        var result = await approvalService.GetManagerToApproveAsync(userIdResult.Value, cancellationToken);

        return result.ToActionResult();
    }

    [HttpPut("[action]/{approvalId:guid}")]
    public async Task<IActionResult> UpdateStatus(Guid approvalId, [FromQuery] ApprovalStatusEnum newStatus, CancellationToken cancellationToken = default)
    {
        var userIdResult = User.GetUserId();
        if (!userIdResult.IsSuccess) return ((Result)userIdResult).ToActionResult();

        Result result =
            await approvalService.UpdateStatus(approvalId, newStatus, userIdResult.Value, cancellationToken);
        
        return result.ToActionResult();
    }

    [HttpPost("[action]")]
    public async Task<IActionResult> Add([FromBody] ApprovalAddDTO toAddData, CancellationToken cancellationToken = default)
    {
        var userIdResult = User.GetUserId();
        if (!userIdResult.IsSuccess) return ((Result)userIdResult).ToActionResult();
        
        Result result = await approvalService.AddAsync(toAddData, userIdResult.Value, cancellationToken);
        
        return result.ToActionResult();
    }
    
}