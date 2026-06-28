using HR_System.Core.common;
using HR_System.Core.DTO.LazyLoading;
using HR_System.Core.DTO.Task;
using HR_System.Core.Enums;
using HR_System.Core.Interfaces.ServiceContracts.ITaskServices;
using HR_System.ExtensionMethods;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HR_System.Controllers;

public class TasksController(ITasksService tasksesService) : ApplicationControllerBase
{
    [HttpGet]
    [Authorize]
    public async Task<ActionResult<IReadOnlyList<TaskDTO>>> GetUserTasks([FromQuery]LazyDTO lazyData, CancellationToken cancellationToken = default)
    {
        var userId = User.GetUserId();
        if (!userId.IsSuccess)
            return ((Result)userId).ToActionResult();

        var userTasks = await tasksesService.LazyGetUserTasksAsync(userId.Value, lazyData, cancellationToken);
        return userTasks.ToActionResult();
    }

    [HttpPut("[action]/{taskId:guid}")]
    public async Task<IActionResult> Update([FromRoute]Guid taskId, [FromQuery] TaskStatusEnum newStatus, CancellationToken cancellationToken = default)
    {
        var userId = User.GetUserId();
        if (!userId.IsSuccess)
            return ((Result)userId).ToActionResult();

        Result result = await tasksesService.UpdateStatusAsync(userId.Value, taskId, newStatus, cancellationToken);
        return result.ToActionResult();
    }

    [HttpPost("[action]")]
    public async Task<IActionResult> Add(TaskAddDTO toTaskAdd, CancellationToken cancellationToken = default)
    {
        var userId = User.GetUserId();
        if (!userId.IsSuccess)
            return ((Result)userId).ToActionResult();
        
        Result result = await tasksesService.AddAsync(toTaskAdd,userId.Value, cancellationToken);
        return result.ToActionResult();
    }
}