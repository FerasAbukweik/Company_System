using HR_System.Core.common;

namespace HR_System.Core.Interfaces.ServiceContracts.ICookieServices;

public interface IGetCookieService
{
    Result<T> GetValue<T>(string key);
    Result<string> Get(string key);
}