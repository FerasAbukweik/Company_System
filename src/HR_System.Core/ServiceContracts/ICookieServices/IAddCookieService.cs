using HR_System.Core.DTO;
using HR_System.Core.Helpers;

namespace HR_System.Core.ServiceContracts.ICookieServices;

public interface IAddCookieService
{
    Result AddToCookies(string key, string toAdd, int lifetimeInMinutes);
    Task<Result<AccessAndRefreshTokenDTO>> AddTokensToCookies(AccessAndRefreshTokenDTO? tokens = null);
}