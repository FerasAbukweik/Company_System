using HR_System.Core.common;
using HR_System.Core.Domain.Entities;

namespace HR_System.Core.Interfaces.ServiceContracts.ITokenServices;

public interface ICheckTokenService
{
    Task<Result<RefreshToken>> IsRefreshTokenValid(Guid userId, CancellationToken cancellationToken = default);
}