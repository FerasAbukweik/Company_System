using HR_System.Core.common;
using HR_System.Core.DTO.LazyLoading;
using HR_System.Core.DTO.Task;
using HR_System.Core.Enums;
using HR_System.Core.Interfaces.ServiceContracts.ITaskServices;
using HR_System.ExtensionMethods;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HR_System.Controllers;

public class TasksController(ITasksService tasksService,
    ILogger<TasksController> logger) : ApplicationControllerBase
{
    [HttpGet]
    [Authorize]
    public async Task<ActionResult<IReadOnlyList<TaskDTO>>> GetUserTasks([FromQuery]LazyDTO lazyData, CancellationToken cancellationToken = default)
    {
        var userId = User.GetUserId();
        if (!userId.IsSuccess)
            return ((Result)userId).ToActionResult(logger);

        var userTasks = await tasksService.LazyGetUserTasksAsync(userId.Value, lazyData, cancellationToken);
        return userTasks.ToActionResult(logger);
    }

    [HttpPut("[action]/{taskId:guid}")]
    [Authorize]
    public async Task<IActionResult> Update([FromRoute]Guid taskId, [FromQuery] TaskStatusEnum newStatus, CancellationToken cancellationToken = default)
    {
        var userId = User.GetUserId();
        if (!userId.IsSuccess)
            return ((Result)userId).ToActionResult(logger);

        Result result = await tasksService.UpdateStatusAsync(userId.Value, taskId, newStatus, cancellationToken);
        return result.ToActionResult(logger);
    }

    [HttpPost("[action]")]
    [Authorize]
    public async Task<IActionResult> Add(TaskAddDTO toTaskAdd, CancellationToken cancellationToken = default)
    {
        var userId = User.GetUserId();
        if (!userId.IsSuccess)
            return ((Result)userId).ToActionResult(logger);
        
        Result result = await tasksService.AddAsync(toTaskAdd,userId.Value, cancellationToken);
        return result.ToActionResult(logger);
    }
}