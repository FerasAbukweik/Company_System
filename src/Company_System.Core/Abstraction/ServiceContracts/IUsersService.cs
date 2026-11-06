using HR_System.Core.DTO.Auth;
using HR_System.Core.Domain.Identity;
using HR_System.Core.DTO.Users;
using HR_System.Core.DTO.LazyLoading;
using HR_System.Core.Result;

namespace HR_System.Core.Abstraction.ServiceContracts;

public interface IUsersService
{
    /// <summary>
    /// Uploads the profile image, creates the account (with its image row) and adds the user to the organization hierarchy.
    /// The uploaded file is removed again if a later step fails.
    /// </summary>
    /// <param name="toCreateData">The employee's account data, profile image and parent in the hierarchy.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>A result containing the created user, or a failure (e.g. a conflict when the email, phone number or username is already used).</returns>
    Task<Result<ApplicationUser>> AddEmployeeAsync(AddEmployeeDTO toCreateData, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the next section of employees for the admin table, ordered by user id.
    /// </summary>
    Task<Result<EmployeePageDTO>> LazyGetEmployeesAsync(
        EmployeeQueryDTO lazyData,
        CancellationToken cancellationToken = default);
    Task<Result<UserDTO>> GetUserAsync(Guid userId, CancellationToken cancellationToken = default);

    Task<Result<EmployeeDTO>> UpdateEmployeeAsync(Guid userId, EmployeeUpdateDTO data, CancellationToken cancellationToken = default);

    Task<HR_System.Core.Result.Result> DeactivateEmployeeAsync(Guid userId, Guid currentUserId, CancellationToken cancellationToken = default);

}
