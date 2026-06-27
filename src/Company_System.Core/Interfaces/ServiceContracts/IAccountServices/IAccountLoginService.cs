using HR_System.Core.common;
using HR_System.Core.DTO;

namespace HR_System.Core.Interfaces.ServiceContracts.IAccountServices;

public interface IAccountLoginService
{
    Task<Result<AccessAndRefreshTokenDTO>> LoginAsync(LoginDTO loginData, CancellationToken cancellationToken = default);
}