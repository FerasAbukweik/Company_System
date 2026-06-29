using HR_System.Core.common;
using HR_System.Core.DTO.Token;

namespace HR_System.Core.Interfaces.ServiceContracts;

public interface ICookiesServices
{
    Result Add(string key, string toAdd, int lifetimeInMinutes);
    Result AddTokens(AccessAndRefreshTokenDTO tokens);
    Result<T> GetValue<T>(string key);
    Result<string> Get(string key);
    
}