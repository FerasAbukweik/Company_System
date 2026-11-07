using HR_System.Core.Abstraction.ServiceContracts;
using HR_System.Core.DTO.Payments;
using HR_System.Core.Enums;
using HR_System.Core.Result;
using HR_System.ExtensionMethods;
using HR_System.Filters;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace HR_System.Controllers;

public class PaymentsController(
    IPaymentsService paymentsService) : ApplicationControllerBase
{
    [HttpGet]
    [Authorize]
    public async Task<ActionResult<UserPaymentPageDTO>> GetMyPayments(
        [FromQuery] UserPaymentQueryDTO query,
        CancellationToken cancellationToken = default)
    {
        var userId = User.GetUserId();
        if (!userId.IsSuccess) return ((Result)userId).ToActionResult();

        return (await paymentsService.GetUserPaymentPageAsync(userId.Value, query, cancellationToken)).ToActionResult();
    }

    [HttpGet("[action]")]
    [Authorize]
    public async Task<ActionResult<UserPaymentOverviewDTO>> GetMyOverview(CancellationToken cancellationToken = default)
    {
        var userId = User.GetUserId();
        if (!userId.IsSuccess) return ((Result)userId).ToActionResult();

        return (await paymentsService.GetUserOverviewAsync(userId.Value, cancellationToken)).ToActionResult();
    }

    [HttpGet("[action]/{userId:guid}")]
    [Authorize(Roles = nameof(RolesEnum.Admin))]
    public async Task<ActionResult<IReadOnlyList<PaymentDTO>>> GetUserPayments(
        [FromRoute] Guid userId,
        [FromQuery] int year,
        [FromQuery] PaymentStateEnum? state = null,
        CancellationToken cancellationToken = default)
    {
        var result = await paymentsService.GetUserPaymentsAsync(userId, year, state, cancellationToken);

        return result.ToActionResult();
    }

    [HttpPut("[action]/{paymentId:guid}")]
    [Authorize(Roles = nameof(RolesEnum.Admin))]
    [Transactional]
    public async Task<ActionResult<PaymentDTO>> UpdateState(
        [FromRoute] Guid paymentId,
        [FromQuery, BindRequired] PaymentStateEnum state,
        CancellationToken cancellationToken = default)
    {
        var result = await paymentsService.UpdateStateAsync(paymentId, state, cancellationToken);

        return result.ToActionResult();
    }
    [HttpGet("[action]")]
    [Authorize(Roles = nameof(RolesEnum.Admin))]
    public async Task<ActionResult<AdminPaymentSummaryDTO>> GetSummary(CancellationToken cancellationToken = default)
    {
        return (await paymentsService.GetAdminSummaryAsync(cancellationToken)).ToActionResult();
    }

    [HttpGet("GetPayments")]
    [Authorize(Roles = nameof(RolesEnum.Admin))]
    public async Task<ActionResult<AdminPaymentPageDTO>> GetPayments([FromQuery] PaymentQueryDTO query, CancellationToken cancellationToken = default)
    {
        return (await paymentsService.GetPaymentsAsync(query, cancellationToken)).ToActionResult();
    }

    [HttpPut("UpdateStates")]
    [Authorize(Roles = nameof(RolesEnum.Admin))]
    [Transactional]
    public async Task<ActionResult<IReadOnlyList<PaymentDTO>>> UpdateStates([FromBody] Guid[] paymentIds,
        [FromQuery, BindRequired] PaymentStateEnum state, CancellationToken cancellationToken = default)
    {
        return (await paymentsService.UpdateStatesAsync(paymentIds, state, cancellationToken)).ToActionResult();
    }
}
