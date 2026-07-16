using System.Collections.Immutable;
using System.Net;
using HR_System.Core.common;
using HR_System.Core.Domain.Entities;
using HR_System.Core.DTO.Activity;
using HR_System.Core.DTO.Approval;
using HR_System.Core.DTO.LazyLoading;
using HR_System.Core.Enums;
using HR_System.Core.helpers;
using HR_System.Core.Interfaces.RepositoryContracts;
using HR_System.Core.Interfaces.ServiceContracts;
using Microsoft.Extensions.Logging;

namespace HR_System.Infrastructure.Services;

public class ApprovalService(IApprovalRepository approvalRepository,
    IActivitiesService activitiesService,
    IOrganizationHierarchyRepository hierarchyRepository,
    ILogger<ApprovalService> logger) : IApprovalService
{
    public async Task<Result<IReadOnlyList<ToApproveDTO>>> GetNeedsApprovalAsync(LazyDTO lazyData, Guid userId, CancellationToken cancellationToken = default)
    {
        var result = await approvalRepository.LazyGetApprovals( lazyData,
            (a => (a.ManagerId == userId && a.Status == ApprovalStatusEnum.Pending)),
            [(a => a.Task!), (a => a.UserRequesting!)],
            cancellationToken);

        return Result<IReadOnlyList<ToApproveDTO>>.Success(result.Select(a => a.ToToApprovalDTO()).ToImmutableList());
    }
    public async Task<Result<IReadOnlyList<RequestedApprovalDTO>>> GetRequested(LazyDTO lazyData, Guid userId, CancellationToken cancellationToken = default)
    {
        var result = await approvalRepository.LazyGetApprovals(lazyData, 
            (a => a.UserRequestingId == userId),
            [(a => a.Task!)],
            cancellationToken);

        return Result<IReadOnlyList<RequestedApprovalDTO>>.Success(result.Select(a => a.ToRequestedApprovalDTO()).ToImmutableList());
    }

    public async Task<Result<RequestedApprovalDTO>> UpdateStatus(Guid approvalId, ApprovalStatusEnum newStatus, Guid currentUserId,
        CancellationToken cancellationToken = default)
    {
        var updated = await approvalRepository.UpdateStatus(approvalId, newStatus, cancellationToken);
        if (updated is null)
            return Result<RequestedApprovalDTO>.Failure("Failed to update approval or approval doesnt exist");

        if (updated.ManagerId != currentUserId)
        {
            logger.LogWarning("{sericeName}.{methodName} user with id {currUserId} tried updating approval for user with id {otherUserId}",
                nameof(ApprovalService), nameof(UpdateStatus), currentUserId, updated.UserRequestingId);
            return Result<RequestedApprovalDTO>.Failure("Unauthorized", HttpStatusCode.Unauthorized);
        }

        if (!await approvalRepository.SaveChangesAsync(cancellationToken))
        {
            logger.LogError("{serviceName}.{methodName} failed saving changes to DB",
                nameof(ApprovalService), nameof(UpdateStatus));
            return Result<RequestedApprovalDTO>.Failure("Failed saving Data to DB");
        }
        
        logger.LogInformation("{serviceName}.{methodName} user with Id: {currUserId} updated approval status with id: {approvalId}",
            nameof(ApprovalService), nameof(UpdateStatus), currentUserId, updated.Id);
        
        return  Result<RequestedApprovalDTO>.Success(updated.ToRequestedApprovalDTO());
    }

    public async Task<Result<ToApproveDTO>> AddAsync(ApprovalAddDTO toAddApproval, Guid currUserId, Guid managerUserId, CancellationToken cancellationToken = default)
    {
        var toAdd = new Approval()
        {
            ManagerId = managerUserId,
            Type = toAddApproval.Type,
            TaskId = toAddApproval.TaskId,
            UserRequestingId = currUserId,
        };
        
        // add to DB
        approvalRepository.Add(toAdd);
        
        // save changes
        if (!await approvalRepository.SaveChangesAsync(cancellationToken))
        {
            logger.LogError("{serviceName}.{methodName} failed saving changes to DB",
                nameof(ApprovalService), nameof(AddAsync));
            return Result<ToApproveDTO>.Failure("Failed saving Data to DB");
        }
        
        var toAddWithInclude = await approvalRepository.FilterAsync(a => a.Id == toAdd.Id,
            [a => a.Task!, a => a.UserRequesting!, a => a.Manager!],
            cancellationToken
        );

        if (!toAddWithInclude.Any())
        {
            logger.LogError("{serviceName}.{methodName} failed fetching approval with id: {approvalId}",
                nameof(ApprovalService), nameof(AddAsync), toAdd.Id);
            return Result<ToApproveDTO>.Failure("no changes happened to DB");
        }

        // add activity
        var addActivityResult = await activitiesService.AddAsync(new ActivityAddDTO()
        {
            Type = ActivityTypeEnum.ApprovalPending,
            Title = ActivityTextGenerator.GetApprovalTitle(toAddWithInclude[0]),
            Description = ActivityTextGenerator.GetApprovalDescription(toAddWithInclude[0]),
        }, currUserId, cancellationToken);
        
        if(!addActivityResult.IsSuccess)
            return addActivityResult.MapFailure<ToApproveDTO>();
        
        logger.LogInformation("{serviceName}.{methodName} user with id: {userId} added approval with id: {approvalId} and type of {approvalType}",
           nameof(ApprovalService), nameof(AddAsync), currUserId, toAdd.Id, toAdd.Type.ToString());

        return Result<ToApproveDTO>.Success(toAdd.ToToApprovalDTO());
    }

    public async Task<Result<ToApproveDTO>> RequestHoliday(Guid currUserId, CancellationToken cancellationToken = default)
    {
        var userHierarchy = await hierarchyRepository.GetByUserIdAsync(currUserId, cancellationToken);
        if (userHierarchy is null)
        {
            logger.LogError("{serviceName}.{methodName} user with id {currUserId} isnt linked with OrganizationHierarchy table",
                nameof(ApprovalService), nameof(AddAsync), currUserId);
            return Result<ToApproveDTO>.Failure("User not found in organization hierarchy", HttpStatusCode.BadRequest);
        }

        if (userHierarchy.Parent is null)
        {
            logger.LogError("{serviceName}.{methodName} user with id {currUserId} doesnt have a father or may be CEO/admin",
                nameof(ApprovalService), nameof(AddAsync), currUserId);
            return Result<ToApproveDTO>.Failure("missing parent in userHierarchy", HttpStatusCode.BadRequest);
        }

        return await AddAsync(new ApprovalAddDTO()
            {
                Type = ApprovalTypeEnum.Holiday
            },
            currUserId,
            userHierarchy.Parent.UserId,
            cancellationToken);
    }
}