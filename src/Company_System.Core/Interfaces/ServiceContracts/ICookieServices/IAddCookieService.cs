using HR_System.Core.common;
using HR_System.Core.DTO;
using HR_System.Core.DTO.Token;

namespace HR_System.Core.Interfaces.ServiceContracts.ICookieServices;

public interface IAddCookieService
{
    Result Add(string key, string toAdd, int lifetimeInMinutes);
    Result AddTokens(AccessAndRefreshTokenDTO tokens);
}