using HR_System.Core.common;
using HR_System.Core.DTO.Approval;

namespace HR_System.Core.Interfaces.ServiceContracts.IApprovalService;

public interface IApprovalSetService
{
    Task<Result<ApprovalDTO>> AddAsync(ApprovalAddDTO toAddApproval,Guid userId, CancellationToken cancellationToken = default);
}