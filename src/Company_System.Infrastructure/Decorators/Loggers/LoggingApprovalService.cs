using HR_System.Core.Abstraction.ServiceContracts;
using HR_System.Core.DTO.Approval;
using HR_System.Core.DTO.LazyLoading;
using HR_System.Core.Enums;
using HR_System.Core.Result;
using HR_System.Infrastructure.Services;
using Microsoft.Extensions.Logging;

namespace HR_System.Infrastructure.Decorators.Loggers;

public class LoggingApprovalService(
    IApprovalService inner,
    ILogger<LoggingApprovalService> logger) : IApprovalService
{
    private const string ServiceName = nameof(ApprovalService);

    public async Task<Result<IReadOnlyList<ToApproveDTO>>> GetNeedsApprovalAsync(LazyDTO lazyData, Guid userId, CancellationToken cancellationToken = default)
    {
        var result = await inner.GetNeedsApprovalAsync(lazyData, userId, cancellationToken);
        logger.LogResult(result, ServiceName, nameof(GetNeedsApprovalAsync), $"UserId: {userId}", logSuccess: false);
        return result;
    }

    public async Task<Result<IReadOnlyList<RequestedApprovalDTO>>> GetRequested(LazyDTO lazyData, Guid userId, CancellationToken cancellationToken = default)
    {
        var result = await inner.GetRequested(lazyData, userId, cancellationToken);
        logger.LogResult(result, ServiceName, nameof(GetRequested), $"UserId: {userId}", logSuccess: false);
        return result;
    }

    public async Task<Result<RequestedApprovalDTO>> UpdateStatus(Guid approvalId, ApprovalStatusEnum newStatus, Guid currentUserId, CancellationToken cancellationToken = default)
    {
        var result = await inner.UpdateStatus(approvalId, newStatus, currentUserId, cancellationToken);
        logger.LogResult(result, ServiceName, nameof(UpdateStatus),
            $"ApprovalId: {approvalId}, NewStatus: {newStatus}, CurrentUserId: {currentUserId}");
        return result;
    }

    public async Task<Result<ToApproveDTO>> AddAsync(ApprovalAddDTO toAddApproval, Guid userId, Guid managerUserId, CancellationToken cancellationToken = default)
    {
        var result = await inner.AddAsync(toAddApproval, userId, managerUserId, cancellationToken);
        logger.LogResult(result, ServiceName, nameof(AddAsync),
            $"UserId: {userId}, ManagerId: {managerUserId}, Type: {toAddApproval.Type}, ApprovalId: {result.Value?.Id}");
        return result;
    }

    public async Task<Result<ToApproveDTO>> RequestHoliday(Guid currUserId, CancellationToken cancellationToken = default)
    {
        var result = await inner.RequestHoliday(currUserId, cancellationToken);
        logger.LogResult(result, ServiceName, nameof(RequestHoliday), $"UserId: {currUserId}");
        return result;
    }
}
