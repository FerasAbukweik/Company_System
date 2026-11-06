using HR_System.Core.DTO.Auth;
using HR_System.Core.Result;

namespace HR_System.Core.Abstraction.ServiceContracts;

public interface IAuthService
{
    /// <summary>
    /// Validates the credentials, issues a new access/refresh token pair and stores both in the client cookies.
    /// A locked account is rejected, and failed attempts count towards locking it.
    /// </summary>
    /// <param name="loginData">The email and password to check.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>A result containing the logged-in user's data if successful.</returns>
    Task<Result<UserDTO>> LoginAsync(LoginDTO loginData, CancellationToken cancellationToken = default);

    /// <summary>
    /// Removes the access and refresh token cookies. Succeeds even if the user is already logged out.
    /// </summary>
    /// <returns>A successful result.</returns>
    Result.Result Logout();

    /// <summary>
    /// Rotates the refresh token found in the cookies and stores the new access/refresh token pair in the cookies.
    /// </summary>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>A plain result: the new tokens only go to the cookies, never to the response body.</returns>
    Task<Result.Result> RefreshTokensAsync(CancellationToken cancellationToken = default);
}
