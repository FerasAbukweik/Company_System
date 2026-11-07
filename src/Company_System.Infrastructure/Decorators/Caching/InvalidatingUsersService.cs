using HR_System.Core.DTO.Auth;
using HR_System.Core.Abstraction.ServiceContracts;
using HR_System.Core.Domain.Identity;
using HR_System.Core.DTO.LazyLoading;
using HR_System.Core.DTO.Users;
using HR_System.Core.Result;

namespace HR_System.Infrastructure.Decorators.Caching;

/// <summary>Employee profile changes also change names, positions, and pictures cached by the org tree.</summary>
public class InvalidatingUsersService(IUsersService inner, IRedisService redis) : IUsersService
{
    public Task<Result<ApplicationUser>> AddEmployeeAsync(AddEmployeeDTO data, CancellationToken cancellationToken = default)
        => inner.AddEmployeeAsync(data, cancellationToken);

    public Task<Result<EmployeeSummaryDTO>> GetEmployeeSummaryAsync(CancellationToken cancellationToken = default)
        => inner.GetEmployeeSummaryAsync(cancellationToken);

    public Task<Result<EmployeePageDTO>> LazyGetEmployeesAsync(EmployeeQueryDTO data, CancellationToken cancellationToken = default)
        => inner.LazyGetEmployeesAsync(data, cancellationToken);

    public Task<Result<UserDTO>> GetUserAsync(Guid userId, CancellationToken cancellationToken = default)
        => inner.GetUserAsync(userId, cancellationToken);

    public Task<Result> DeactivateEmployeeAsync(Guid userId, Guid currentUserId, CancellationToken cancellationToken = default)
        => inner.DeactivateEmployeeAsync(userId, currentUserId, cancellationToken);

    public async Task<Result<EmployeeDTO>> UpdateEmployeeAsync(Guid userId, EmployeeUpdateDTO data, CancellationToken cancellationToken = default)
    {
        var result = await inner.UpdateEmployeeAsync(userId, data, cancellationToken);
        if (!result.IsSuccess) return result;

        try
        {
            await redis.RemoveByPrefixAsync("hierarchy");
        }
        catch (Exception)
        {
            // LoggingRedisService reports cache failures. They must not turn a saved edit into an error.
        }

        return result;
    }
}
