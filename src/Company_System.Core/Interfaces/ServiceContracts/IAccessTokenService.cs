using HR_System.Core.common;
using HR_System.Core.Domain.Identity;

namespace HR_System.Core.Interfaces.ServiceContracts;

public interface IAccessTokenService
{
    /// <summary>
    /// Generates a JWT access token containing user identity and role claims.
    /// </summary>
    /// <param name="user">The user entity for whom the token is generated.</param>
    /// <returns>A result containing the serialized JWT string if successful.</returns>
    Task<Result<string>> GenerateAccessTokenAsync(ApplicationUser user);
}