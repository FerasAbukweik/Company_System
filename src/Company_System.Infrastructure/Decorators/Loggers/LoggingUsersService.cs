using HR_System.Core.DTO.Auth;
using HR_System.Core.Abstraction.ServiceContracts;
using HR_System.Core.Domain.Identity;
using HR_System.Core.DTO.Users;
using HR_System.Core.DTO.LazyLoading;
using HR_System.Core.Result;
using HR_System.Infrastructure.Services;
using Microsoft.Extensions.Logging;

namespace HR_System.Infrastructure.Decorators.Loggers;

public class LoggingUsersService(
    IUsersService inner,
    ILogger<LoggingUsersService> logger) : IUsersService
{
    private const string ServiceName = nameof(UsersService);

    public async Task<Result<ApplicationUser>> AddEmployeeAsync(AddEmployeeDTO toCreateData, CancellationToken cancellationToken = default)
    {
        var result = await inner.AddEmployeeAsync(toCreateData, cancellationToken);
        // credentials (email / password) are deliberately not logged
        logger.LogResult(result, ServiceName, nameof(AddEmployeeAsync),
            $"UserName: {toCreateData.UserName}, Position: {toCreateData.Position}, ParentId: {toCreateData.ParentId}, UserId: {result.Value?.Id}");
        return result;
    }

    public async Task<Result<EmployeePageDTO>> LazyGetEmployeesAsync(
        EmployeeQueryDTO lazyData,
        CancellationToken cancellationToken = default)
    {
        var result = await inner.LazyGetEmployeesAsync(lazyData, cancellationToken);

        logger.LogResult(result, ServiceName, nameof(LazyGetEmployeesAsync),
            $"Taken: {lazyData.Taken}, SectionSize: {lazyData.SectionSize}", logSuccess: false);

        return result;
    }
    public async Task<Result<UserDTO>> GetUserAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var result = await inner.GetUserAsync(userId, cancellationToken);
        logger.LogResult(result, ServiceName, nameof(GetUserAsync), $"UserId: {userId}", logSuccess: false);
        return result;
    }

    public async Task<Result<EmployeeDTO>> UpdateEmployeeAsync(Guid userId, EmployeeUpdateDTO data, CancellationToken cancellationToken = default)
    {
        var result = await inner.UpdateEmployeeAsync(userId, data, cancellationToken);
        logger.LogResult(result, ServiceName, nameof(UpdateEmployeeAsync), $"UserId: {userId}");
        return result;
    }

    public async Task<Result> DeactivateEmployeeAsync(Guid userId, Guid currentUserId, CancellationToken cancellationToken = default)
    {
        var result = await inner.DeactivateEmployeeAsync(userId, currentUserId, cancellationToken);
        logger.LogResult(result, ServiceName, nameof(DeactivateEmployeeAsync), $"UserId: {userId}, CurrentUserId: {currentUserId}");
        return result;
    }
}
