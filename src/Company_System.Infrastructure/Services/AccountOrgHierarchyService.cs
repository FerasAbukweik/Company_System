using HR_System.Core.common;
using HR_System.Core.Domain.Identity;
using HR_System.Core.DTO.Account;
using HR_System.Core.DTO.OrganizationHierarchy;
using HR_System.Core.Interfaces.RepositoryContracts;
using HR_System.Core.Interfaces.ServiceContracts;

namespace HR_System.Infrastructure.Services;

public class AccountOrgHierarchyService(
    IAccountService accountService,
    IOrganizationHierarchyService hierarchyService,
    IApplicationUsersRepository applicationUsersRepository,
    IImageService imageService
    ) : IAccountOrgHierarchyService
{
    public async Task<Result<ApplicationUser>> AddEmployee(AddEmployeeDTO toCreateData, CancellationToken cancellationToken = default)
    {
        // upload image to cloudinary
        var uploadImageResult = await imageService.Upload(toCreateData.Image);
        if(uploadImageResult.Error != null)
            return Result<ApplicationUser>.Failure(uploadImageResult.Error.Message);

        // create strategy to prevent saving changes to DB in case something went wrong 

            // create Application User
            var createAccountResult = await accountService.CreateAccountAsync(
                toCreateData,
                uploadImageResult.SecureUrl.AbsoluteUri,
                uploadImageResult.PublicId,
                cancellationToken);
            
            if(!createAccountResult.IsSuccess)
                return createAccountResult.MapFailure<ApplicationUser>();

            // add user we created to Org Hierarchy
            var addUserToHierarchyResult = await hierarchyService.AddAsync(new OrganizationHierarchyAddDTO()
            {
                ParentId = toCreateData.ParentId,
                UserId = createAccountResult.Value!.Id
            }, cancellationToken);
            
            if(!addUserToHierarchyResult.IsSuccess)
                return addUserToHierarchyResult.MapFailure<ApplicationUser>();

            
            // return result
            return Result<ApplicationUser>.Success(createAccountResult.Value!);

    }
}