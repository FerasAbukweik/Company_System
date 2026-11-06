using HR_System.Core.Domain.Identity;
using HR_System.Core.DTO.Users;
using HR_System.Core.Result;

namespace HR_System.Core.Abstraction.ServiceContracts;

public interface IAccountOrgHierarchyService
{
    /// <summary>
    /// Creates an employee account and adds the new user to the organization hierarchy.
    /// </summary>
    /// <param name="toCreateData">The data of the employee to create, including the parent in the hierarchy.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>A result containing the created user if successful.</returns>
    Task<Result<ApplicationUser>> AddEmployee(AddEmployeeDTO toCreateData, CancellationToken cancellationToken = default);
}
