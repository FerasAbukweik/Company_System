namespace HR_System.Core.Abstraction.ServiceContracts;

public interface IClaimsService
{
    /// <summary>
    /// Gets the user name from the claims of the current request's user.
    /// </summary>
    /// <returns>The user name, or "unknown" when there is none.</returns>
    string GetUserName();

}
