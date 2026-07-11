using HR_System.Core.common;
using HR_System.Core.Domain.Identity;
using HR_System.Core.DTO.Account;

namespace HR_System.Core.Interfaces.ServiceContracts;

public interface IAccountOrgHierarchyService
{
    Task<Result<ApplicationUser>> AddEmployee(AddEmployeeDTO toCreateData, CancellationToken cancellationToken = default);
}