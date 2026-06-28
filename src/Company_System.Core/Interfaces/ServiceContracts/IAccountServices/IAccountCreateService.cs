using HR_System.Core.common;
using HR_System.Core.Domain.Identity;
using HR_System.Core.DTO.Auth;

namespace HR_System.Core.Interfaces.ServiceContracts.IAccountServices;

public interface IAccountCreateService
{
    Task<Result<ApplicationUser>> CreateAccountAsync(AccountCreateDTO toAccountCreate, CancellationToken cancellationToken = default);
}