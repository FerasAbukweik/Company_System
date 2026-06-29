using HR_System.Core.common;
using HR_System.Core.DTO.Approval;

namespace HR_System.Core.Interfaces.ServiceContracts.IApprovalService;

public interface IApprovalGetSerive
{
    Task<Result<IReadOnlyList<ApprovalDTO>>> GetNeedsApprovalAsync(Guid userId, CancellationToken cancellationToken = default);
}