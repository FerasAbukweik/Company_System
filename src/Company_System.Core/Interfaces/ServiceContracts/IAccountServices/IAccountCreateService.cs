using HR_System.Core.common;
using HR_System.Core.Domain.Idnetity;
using HR_System.Core.DTO;
using HR_System.Core.DTO.Auth;

namespace HR_System.Core.Interfaces.ServiceContracts.IAccountServices;

public interface IAccountCreateService
{
    Task<Result<ApplicationUser>> CreateAccountAsync(CreateAccountDTO toCreate, CancellationToken cancellationToken = default);
}