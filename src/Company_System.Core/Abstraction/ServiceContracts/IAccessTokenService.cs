using HR_System.Core.Domain.Identity;
using HR_System.Core.DTO.Token;
using HR_System.Core.Result;

namespace HR_System.Core.Abstraction.ServiceContracts;

public interface IAccessTokenService
{
    /// <summary>
    /// Generates a JWT access token containing user identity and role claims.
    /// </summary>
    /// <param name="user">The user entity for whom the token is generated.</param>
    /// <returns>A result containing the serialized JWT string if successful.</returns>
    Task<Result<AccessTokenResult>> GenerateAccessTokenAsync(ApplicationUser user);
}