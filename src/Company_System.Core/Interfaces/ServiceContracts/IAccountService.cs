using HR_System.Core.common;
using HR_System.Core.Domain.Identity;
using HR_System.Core.DTO.Auth;
using HR_System.Core.DTO.Token;

namespace HR_System.Core.Interfaces.ServiceContracts;

public interface IAccountService
{
    Task<Result<ApplicationUser>> CreateAccountAsync(AccountCreateDTO toAccountCreate, CancellationToken cancellationToken = default);
    Task<Result<AccessAndRefreshTokenDTO>> LoginAsync(LoginDTO loginData, CancellationToken cancellationToken = default);
    
}