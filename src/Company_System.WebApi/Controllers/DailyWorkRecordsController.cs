using HR_System.Core.DTO.LazyLoading;
using HR_System.Core.Abstraction.ServiceContracts;
using HR_System.Core.DTO.Attendance;
using HR_System.Core.Enums;
using HR_System.Core.Result;
using HR_System.ExtensionMethods;
using HR_System.Filters;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HR_System.Controllers;

public class DailyWorkRecordsController(
    IDailyWorkRecordsService recordsService) : ApplicationControllerBase
{
    [HttpGet("[action]")]
    [Authorize(Roles = nameof(RolesEnum.Admin))]
    public async Task<ActionResult<AttendanceDayPageDTO>> GetDayRecords(
        [FromQuery] DateOnly date,
        [FromQuery] LazyDTO lazyData,
        CancellationToken cancellationToken = default)
    {
        var result = await recordsService.GetDayRecordsAsync(date, lazyData, cancellationToken);

        return result.ToActionResult();
    }

    [HttpGet]
    [Authorize]
    public async Task<ActionResult<IReadOnlyList<DailyWorkRecordDTO>>> GetMyRecords(
        [FromQuery] DateOnly from,
        [FromQuery] DateOnly to,
        CancellationToken cancellationToken = default)
    {
        var userId = User.GetUserId();
        if (!userId.IsSuccess)
            return ((Result)userId).ToActionResult();

        var result = await recordsService.GetUserRecordsAsync(userId.Value, from, to, cancellationToken);

        return result.ToActionResult();
    }

    [HttpGet("[action]/{userId:guid}")]
    [Authorize(Roles = nameof(RolesEnum.Admin))]
    public async Task<ActionResult<IReadOnlyList<DailyWorkRecordDTO>>> GetUserRecords(
        [FromRoute] Guid userId,
        [FromQuery] DateOnly from,
        [FromQuery] DateOnly to,
        CancellationToken cancellationToken = default)
    {
        var result = await recordsService.GetUserRecordsAsync(userId, from, to, cancellationToken);

        return result.ToActionResult();
    }

    [HttpPost("[action]")]
    [Authorize(Roles = nameof(RolesEnum.Admin))]
    [Transactional]
    public async Task<ActionResult<DailyWorkRecordDTO>> Add(
        [FromBody] DailyWorkRecordSaveDTO data,
        CancellationToken cancellationToken = default)
    {
        var result = await recordsService.AddAsync(data, cancellationToken);

        return result.ToActionResult();
    }

    [HttpPut("[action]/{recordId:guid}")]
    [Authorize(Roles = nameof(RolesEnum.Admin))]
    [Transactional]
    public async Task<ActionResult<DailyWorkRecordDTO>> Update(
        [FromRoute] Guid recordId,
        [FromBody] DailyWorkRecordUpdateDTO data,
        CancellationToken cancellationToken = default)
    {
        var result = await recordsService.UpdateAsync(recordId, data.MinutesWorked, data.State, cancellationToken);

        return result.ToActionResult();
    }
}
