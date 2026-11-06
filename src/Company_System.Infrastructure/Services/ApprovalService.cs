using System.Collections.Immutable;
using System.Net;
using HR_System.Core.Abstraction.RepositoryContracts;
using HR_System.Core.Abstraction.ServiceContracts;
using HR_System.Core.Domain.Entities;
using HR_System.Core.DTO.Activity;
using HR_System.Core.DTO.Approval;
using HR_System.Core.DTO.LazyLoading;
using HR_System.Core.Enums;
using HR_System.Core.Result;

namespace HR_System.Infrastructure.Services;

public class ApprovalService(IApprovalRepository approvalRepository,
    IActivitiesService activitiesService,
    IOrganizationHierarchyRepository hierarchyRepository) : IApprovalService
{
    public async Task<Result<IReadOnlyList<ToApproveDTO>>> GetNeedsApprovalAsync(LazyDTO lazyData, Guid userId, CancellationToken cancellationToken = default)
    {
        var result = await approvalRepository.FilterAsync(
            a => a.ManagerId == userId && a.Status == ApprovalStatusEnum.Pending,
            [a => a.Task!, a => a.UserRequesting!],
            a => a.CreatedOn,
            true,
            lazyData.Taken,
            lazyData.SectionSize,
            cancellationToken);

        return Result<IReadOnlyList<ToApproveDTO>>.Success(result.Select(a => a.ToToApprovalDTO()).ToImmutableList());
    }
    public async Task<Result<IReadOnlyList<RequestedApprovalDTO>>> GetRequested(LazyDTO lazyData, Guid userId, CancellationToken cancellationToken = default)
    {
        var result = await approvalRepository.FilterAsync(
            a => a.UserRequestingId == userId,
            [a => a.Task!],
            a => a.CreatedOn,
            true,
            lazyData.Taken,
            lazyData.SectionSize,
            cancellationToken);

        return Result<IReadOnlyList<RequestedApprovalDTO>>.Success(result.Select(a => a.ToRequestedApprovalDTO()).ToImmutableList());
    }

    public async Task<Result<RequestedApprovalDTO>> UpdateStatus(Guid approvalId, ApprovalStatusEnum newStatus, Guid currentUserId,
        CancellationToken cancellationToken = default)
    {
        // get the approval with the new status applied (not saved yet), then check it before saving
        var updated = await approvalRepository.UpdateStatusAsync(approvalId, newStatus, cancellationToken);
        if (updated is null)
            return Result<RequestedApprovalDTO>.Failure("Failed to update approval or approval doesnt exist");

        // only the manager may change the status, checked before anything is saved
        if (updated.ManagerId != currentUserId)
        {
            return Result<RequestedApprovalDTO>.Failure("Unauthorized", HttpStatusCode.Forbidden);
        }

        // everything is ok, save
        if (!await approvalRepository.SaveChangesAsync(cancellationToken))
        {
            return Result<RequestedApprovalDTO>.Failure("Failed saving Data to DB");
        }


        return Result<RequestedApprovalDTO>.Success(updated.ToRequestedApprovalDTO());
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

        // save changes, the approval has to be in the DB for the include fetch below
        if (!await approvalRepository.SaveChangesAsync(cancellationToken))
        {
            return Result<ToApproveDTO>.Failure("Failed saving Data to DB");
        }

        var toAddWithInclude = await approvalRepository.GetByIdAsync(
            toAdd.Id,
            [a => a.Task!, a => a.UserRequesting!, a => a.Manager!],
            cancellationToken);

        if (toAddWithInclude is null)
        {
            return Result<ToApproveDTO>.Failure("no changes happened to DB");
        }

        // add activity
        var addActivityResult = await activitiesService.AddAsync(new ActivityAddDto()
        {
            Type = ActivityTypeEnum.ApprovalPending,
            ApprovalId = toAddWithInclude.Id,
        }, currUserId, cancellationToken);

        if (!addActivityResult.IsSuccess)
            return addActivityResult.MapFailure<ToApproveDTO>();


        return Result<ToApproveDTO>.Success(toAddWithInclude.ToToApprovalDTO());
    }

    public async Task<Result<ToApproveDTO>> RequestHoliday(Guid currUserId, CancellationToken cancellationToken = default)
    {
        var userHierarchy = await hierarchyRepository.GetByUserIdAsync(currUserId, cancellationToken);
        if (userHierarchy is null)
        {
            return Result<ToApproveDTO>.Failure("User not found in organization hierarchy", HttpStatusCode.BadRequest);
        }

        if (userHierarchy.Parent is null)
        {
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