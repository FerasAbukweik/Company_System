using HR_System.Core.Domain.Idnetity;
using HR_System.Core.DTO;
using HR_System.Core.Helpers;

namespace HR_System.Core.ServiceContracts.IAccountServices;

public interface ICreateAccountService
{
    Task<Result<ApplicationUser>> CreateAccountAsync(CreateAccountDTO toCreate, CancellationToken cancellationToken = default);
}