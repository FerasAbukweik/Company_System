using HR_System.Core.DTO.Auth;
using System.Net;
using System.Linq.Expressions;
using HR_System.Core.Abstraction.RepositoryContracts;
using HR_System.Core.Abstraction.ServiceContracts;
using HR_System.Core.Domain.Entities;
using HR_System.Core.Domain.Identity;
using HR_System.Core.DTO.Users;
using HR_System.Core.DTO.LazyLoading;
using HR_System.Core.DTO.OrganizationHierarchy;
using HR_System.Core.Enums;
using HR_System.Core.Result;
using Microsoft.AspNetCore.Identity;

namespace HR_System.Infrastructure.Services;

public class UsersService(UserManager<ApplicationUser> userManager,
    IApplicationUsersRepository usersRepository,
    IOrganizationHierarchyService hierarchyService,
    IImageService imageService,
    IOrganizationHierarchyRepository hierarchyRepository) : IUsersService
{
    public async Task<Result<ApplicationUser>> AddEmployeeAsync(AddEmployeeDTO toCreateData, CancellationToken cancellationToken = default)
    {
        // upload image to cloudinary
        var uploadImageResult = await imageService.Upload(toCreateData.Image);
        if (uploadImageResult.Error != null)
            return Result<ApplicationUser>.Failure(uploadImageResult.Error.Message);

        // the image row is created with the user (UserId is set when the user is saved)
        var image = new Image()
        {
            PublicId = uploadImageResult.PublicId,
            Url = uploadImageResult.SecureUrl.AbsoluteUri,
            UserId = toCreateData.UserId
        };

        // create Application User
        var createdUserResult = await CreateUserAsync(toCreateData, image, cancellationToken);

        if (!createdUserResult.IsSuccess)
        {
            await imageService.Delete(image.PublicId); // user was not created, don't leave the file in Cloudinary
            return createdUserResult.MapFailure<ApplicationUser>();
        }

        // add user we created to Org Hierarchy
        var addUserToHierarchyResult = await hierarchyService.AddAsync(new OrganizationHierarchyAddDTO()
        {
            ParentId = toCreateData.ParentId,
            UserId = createdUserResult.Value!.Id
        }, cancellationToken);

        if (!addUserToHierarchyResult.IsSuccess)
        {
            // the request transaction rolls the user and image rows back, so remove the file here
            await imageService.Delete(image.PublicId);
            return addUserToHierarchyResult.MapFailure<ApplicationUser>();
        }


        // return result
        return Result<ApplicationUser>.Success(createdUserResult.Value!);
    }


    public async Task<Result<EmployeePageDTO>> LazyGetEmployeesAsync(EmployeeQueryDTO query, CancellationToken cancellationToken = default)
    {
        if (query.Taken < 0 || query.SectionSize < 1 || query.Search?.Length > 100)
            return Result<EmployeePageDTO>.Failure("Invalid employee search or page size", HttpStatusCode.BadRequest);

        var search = query.Search?.Trim().ToUpperInvariant();
        var positions = string.IsNullOrEmpty(search) ? [] : Enum.GetValues<PositionsEnum>()
            .Where(position => position.ToString().ToUpperInvariant().Contains(search)).ToArray();
        Guid? employeeId = Guid.TryParse(search, out var id) ? id : null;
        bool? isActive = search == "ACTIVE" ? true : search == "INACTIVE" ? false : null;

        Expression<Func<ApplicationUser, bool>> predicate = user =>
            (!query.ActiveOnly || (user.OrganizationHierarchy != null && user.OrganizationHierarchy.IsActive)) &&
            (string.IsNullOrEmpty(search) || user.FullName.ToUpper().Contains(search) ||
            (user.UserName != null && user.UserName.ToUpper().Contains(search)) ||
            (user.Email != null && user.Email.ToUpper().Contains(search)) ||
            (user.PhoneNumber != null && user.PhoneNumber.ToUpper().Contains(search)) ||
            positions.Contains(user.Position) ||
            (employeeId.HasValue && user.Id == employeeId.Value) ||
            (isActive.HasValue && (user.OrganizationHierarchy != null && user.OrganizationHierarchy.IsActive) == isActive.Value));

        var summary = await usersRepository.GetSummaryAsync(predicate, cancellationToken);
        var employees = await usersRepository.FilterAsync(predicate,
            include: [user => user.Image, user => user.OrganizationHierarchy],
            orderBy: user => user.CreatedAt,
            orderByDesc: true,
            skip: query.Taken,
            take: query.SectionSize,
            cancellationToken: cancellationToken);

        return Result<EmployeePageDTO>.Success(new EmployeePageDTO
        {
            Items = employees.Select(ToEmployeeDTO).ToList(),
            TotalCount = summary.Headcount,
            Summary = summary
        });
    }


    public async Task<Result<UserDTO>> GetUserAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var user = await usersRepository.GetByIdAsync(userId, [u => u.Image, u => u.OrganizationHierarchy], cancellationToken);
        return user is null
            ? Result<UserDTO>.Failure("Employee not found", HttpStatusCode.NotFound)
            : Result<UserDTO>.Success(user.ToUserDTO());
    }

    public async Task<Result<EmployeeDTO>> UpdateEmployeeAsync(Guid userId, EmployeeUpdateDTO data, CancellationToken cancellationToken = default)
    {
        if (!Enum.IsDefined(data.Position) ||
            data.HourlyRate is < 0 or > 9999999999999999.99m ||
            (data.HourlyRate.HasValue && decimal.Round(data.HourlyRate.Value, 2) != data.HourlyRate.Value) ||
            data.AnnualHolidayAllowance is < 0 or > 366)
            return Result<EmployeeDTO>.Failure("Invalid employee data", HttpStatusCode.BadRequest);

        var user = await usersRepository.GetByIdAsync(userId, [u => u.Image, u => u.OrganizationHierarchy], cancellationToken);
        if (user is null)
            return Result<EmployeeDTO>.Failure("Employee not found", HttpStatusCode.NotFound);

        // Root ownership and admin role changes must not be an incidental directory edit.
        if ((user.Position == PositionsEnum.CEO || data.Position == PositionsEnum.CEO) && user.Position != data.Position)
            return Result<EmployeeDTO>.Failure("The CEO position cannot be reassigned through employee editing", HttpStatusCode.BadRequest);

        if (data.ParentId.HasValue && data.ParentId != user.OrganizationHierarchy?.ParentId)
        {
            var node = user.OrganizationHierarchy;
            if (node is null || !node.IsActive || node.ParentId is null)
                return Result<EmployeeDTO>.Failure("This employee's manager cannot be changed", HttpStatusCode.BadRequest);

            var manager = await hierarchyRepository.GetByIdAsync(data.ParentId.Value, cancellationToken);
            if (manager is null || !manager.IsActive)
                return Result<EmployeeDTO>.Failure("Choose an active manager", HttpStatusCode.BadRequest);

            // Walk upward from the proposed manager to reject self-management and cycles.
            var visited = new HashSet<Guid> { node.Id };
            while (manager is not null)
            {
                if (!visited.Add(manager.Id))
                    return Result<EmployeeDTO>.Failure("An employee cannot report to themselves or their descendants", HttpStatusCode.BadRequest);

                manager = manager.ParentId.HasValue
                    ? await hierarchyRepository.GetByIdAsync(manager.ParentId.Value, cancellationToken)
                    : null;
            }
        }

        var duplicate = await usersRepository.FilterAsync(u => u.Id != userId &&
            (u.NormalizedEmail == data.Email.ToUpper() || u.NormalizedUserName == data.UserName.ToUpper() || u.PhoneNumber == data.PhoneNumber),
            take: 1, cancellationToken: cancellationToken);
        if (duplicate.Count > 0)
            return Result<EmployeeDTO>.Failure("Email, username, or phone number is already in use", HttpStatusCode.Conflict);

        Image? replacement = null;
        if (data.Image is not null)
        {
            var uploaded = await imageService.Upload(data.Image);
            if (uploaded.Error is not null || uploaded.SecureUrl is null || string.IsNullOrEmpty(uploaded.PublicId))
                return Result<EmployeeDTO>.Failure("Failed to upload profile image", HttpStatusCode.BadRequest);

            replacement = new Image { PublicId = uploaded.PublicId, Url = uploaded.SecureUrl.AbsoluteUri, UserId = userId };
        }

        user.FullName = data.FullName;
        user.UserName = data.UserName;
        user.Email = data.Email;
        user.PhoneNumber = data.PhoneNumber;
        user.Position = data.Position;
        user.HourlyRate = data.HourlyRate;
        user.AnnualHolidayAllowance = data.AnnualHolidayAllowance;

        var updated = await userManager.UpdateAsync(user);
        if (!updated.Succeeded)
        {
            if (replacement is not null) await imageService.Delete(replacement.PublicId);
            return Result<EmployeeDTO>.Failure(string.Join(" | ", updated.Errors.Select(error => error.Description)), HttpStatusCode.BadRequest);
        }

        if (data.ParentId.HasValue && data.ParentId != user.OrganizationHierarchy?.ParentId)
        {
            hierarchyRepository.SetParent(user.OrganizationHierarchy!.Id, data.ParentId.Value);
            if (!await hierarchyRepository.SaveChangesAsync(cancellationToken))
                return Result<EmployeeDTO>.Failure("Failed to update employee manager");

            user.OrganizationHierarchy.ParentId = data.ParentId.Value;
        }

        if (replacement is not null)
        {
            try
            {
                await usersRepository.ReplaceImageAsync(userId, replacement, cancellationToken);
                await usersRepository.SaveChangesAsync(cancellationToken);
                user.Image = replacement;
            }
            catch
            {
                await imageService.Delete(replacement.PublicId);
                throw;
            }
        }

        return Result<EmployeeDTO>.Success(ToEmployeeDTO(user));
    }

    public async Task<Result> DeactivateEmployeeAsync(Guid userId, Guid currentUserId, CancellationToken cancellationToken = default)
    {
        var user = await usersRepository.GetByIdAsync(userId, [u => u.OrganizationHierarchy], cancellationToken);
        if (user?.OrganizationHierarchy is null)
            return Result.Failure("Employee not found", HttpStatusCode.NotFound);
        if (userId == currentUserId)
            return Result.Failure("You cannot deactivate your own account", HttpStatusCode.BadRequest);
        if (user.OrganizationHierarchy.ParentId is null)
            return Result.Failure("The root employee cannot be deactivated", HttpStatusCode.BadRequest);

        return await hierarchyService.RemoveAsync(user.OrganizationHierarchy.Id, currentUserId, cancellationToken);
    }

    private static EmployeeDTO ToEmployeeDTO(ApplicationUser user) => new()
    {
        Id = user.Id,
        TreeId = user.OrganizationHierarchy?.Id,
        ParentId = user.OrganizationHierarchy?.ParentId,
        FullName = user.FullName,
        UserName = user.UserName,
        Email = user.Email,
        Phone = user.PhoneNumber,
        ImageUrl = user.Image?.Url,
        Position = user.Position,
        HourlyRate = user.HourlyRate,
        AnnualHolidayAllowance = user.AnnualHolidayAllowance,
        IsActive = user.OrganizationHierarchy?.IsActive ?? false
    };


    // private helper methods
    private async Task<Result<ApplicationUser>> CreateUserAsync(
        UserCreateDTO toCreateData,
        Image image,
        CancellationToken cancellationToken = default)
    {
        // check if user already exists
        var doesUsesExist = await DoesUserExist(toCreateData, cancellationToken);
        if (doesUsesExist.IsSuccess)
            return Result<ApplicationUser>.Failure(doesUsesExist.Value!, HttpStatusCode.Conflict); // return fields used in other users

        // Add user to DB
        var toAddUser = new ApplicationUser()
        {
            Id = toCreateData.UserId,
            UserName = toCreateData.UserName,
            Email = toCreateData.Email,
            PhoneNumber = toCreateData.PhoneNumber,
            FullName = toCreateData.FullName,
            Position = toCreateData.Position,
            Image = image,
            HourlyRate = toCreateData.HourlyRate,
            AnnualHolidayAllowance = toCreateData.AnnualHolidayAllowance,
        };
        var createUserResult = await userManager.CreateAsync(toAddUser, toCreateData.Password);
        if (!createUserResult.Succeeded)
        {
            var errorsString = createUserResult.Errors.Select(e => e.Description);
            return Result<ApplicationUser>.Failure(string.Join(" | ", errorsString));
        }

        // add user to his role
        var addUserToRoleResult = await userManager.AddToRoleAsync(toAddUser, toCreateData.Position == PositionsEnum.CEO ?
            nameof(RolesEnum.Admin) : nameof(RolesEnum.Employee));

        if (!addUserToRoleResult.Succeeded)
        {
            var errorsString = addUserToRoleResult.Errors.Select(e => e.Description);
            return Result<ApplicationUser>.Failure(string.Join(" | ", errorsString));
        }


        return Result<ApplicationUser>.Success(toAddUser);
    }
    private async Task<Result<string>> DoesUserExist(UserCreateDTO toUserCreate, CancellationToken cancellationToken = default)
    {
        // check if user already Exists
        var existingUsers = await usersRepository.FilterAsync((u =>
                (u.UserName!.ToLower() == toUserCreate.UserName.ToLower() ||
                 u.Email!.ToLower() == toUserCreate.Email.ToLower() ||
                 u.PhoneNumber == toUserCreate.PhoneNumber)
            ), cancellationToken: cancellationToken);

        // if user already exist generate error message and return failure
        if (existingUsers.Any())
        {
            bool isEmailUsed = false, isPhoneUsed = false, isUserNameUsed = false;

            // see what is used
            foreach (var user in existingUsers)
            {
                if (user.UserName == toUserCreate.UserName) isUserNameUsed = true;
                if (user.Email == toUserCreate.Email) isEmailUsed = true;
                if (user.PhoneNumber == toUserCreate.PhoneNumber) isPhoneUsed = true;

                if (isEmailUsed && isPhoneUsed && isUserNameUsed) break;
            }

            // collect used fields in list
            var usedFields = new List<string>();
            if (isEmailUsed) usedFields.Add("Email");
            if (isPhoneUsed) usedFields.Add("Phone number");
            if (isUserNameUsed) usedFields.Add("Username");

            // generate error message
            string fieldsText = string.Join(",\n", usedFields);
            string verb = usedFields.Count == 1 ? "\nis already used." : "\nare already used.";
            string errorMessage = $"{fieldsText} {verb}";

            return Result<string>.Success(errorMessage);
        }

        return Result<string>.Failure("");
    }
}