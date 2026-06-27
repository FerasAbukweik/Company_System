using HR_System.Core.common;
using HR_System.Core.DTO;
using HR_System.Core.DTO.Auth;
using HR_System.Core.DTO.Token;

namespace HR_System.Core.Interfaces.ServiceContracts.IAccountServices;

public interface IAccountLoginService
{
    Task<Result<AccessAndRefreshTokenDTO>> LoginAsync(LoginDTO loginData, CancellationToken cancellationToken = default);
}