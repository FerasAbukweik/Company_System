using HR_System.Core.Domain.Idnetity;
using HR_System.Core.Helpers;

namespace HR_System.Core.ServiceContracts.ICurrentUserServices;

public interface IGetCurrentUserService
{
    Result<Guid> GetUserId();
    Task<Result<ApplicationUser>> GetCurrUserObjectAsync();
}