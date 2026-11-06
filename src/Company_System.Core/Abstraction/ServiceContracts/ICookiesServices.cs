using HR_System.Core.DTO.Token;
using HR_System.Core.Result;

namespace HR_System.Core.Abstraction.ServiceContracts;

public interface ICookiesServices
{
    /// <summary>
    /// Adds an HTTP-only, secure cookie to the current response.
    /// </summary>
    /// <param name="key">The name of the cookie.</param>
    /// <param name="toAdd">The value of the cookie.</param>
    /// <param name="lifetimeInMinutes">How many minutes until the cookie expires. A negative value expires it immediately.</param>
    /// <returns>A successful result, or a failure when there is no current HTTP context.</returns>
    Result.Result Add(string key, string toAdd, int lifetimeInMinutes);

    /// <summary>
    /// Adds the access token and the refresh token as cookies, each with its own lifetime.
    /// </summary>
    /// <param name="tokens">The access and refresh tokens to store.</param>
    /// <returns>A successful result, or the failure of the first cookie that couldn't be added.</returns>
    Result.Result AddTokens(AccessAndRefreshTokenDTO tokens);

    /// <summary>
    /// Reads a cookie from the current request and converts it to the requested type.
    /// </summary>
    /// <typeparam name="T">The type to convert the cookie value to.</typeparam>
    /// <param name="key">The name of the cookie.</param>
    /// <returns>A result containing the converted value, or a failure when the cookie is missing or can't be converted.</returns>
    Result<T> GetValue<T>(string key);

    /// <summary>
    /// Reads a cookie from the current request as text.
    /// </summary>
    /// <param name="key">The name of the cookie.</param>
    /// <returns>A result containing the cookie value, or a failure when the cookie is missing.</returns>
    Result<string> Get(string key);

    /// <summary>
    /// Removes a cookie by expiring it on the current response.
    /// </summary>
    /// <param name="key">The name of the cookie.</param>
    /// <returns>A result containing the value the cookie had, or a failure when it didn't exist.</returns>
    Result<string> Remove(string key);

    /// <summary>
    /// Removes both the access token and the refresh token cookies.
    /// </summary>
    /// <returns>A result containing the removed refresh token value, or the failure of the access token removal when that cookie was missing.</returns>
    Result<string> RemoveTokens();
}
