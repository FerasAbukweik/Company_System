using System.Net;
using HR_System.Core.common;
using HR_System.Core.Domain.Identity;
using HR_System.Core.DTO.Account;
using HR_System.Core.DTO.Auth;
using HR_System.Core.Enums;
using HR_System.Core.Interfaces.RepositoryContracts;
using HR_System.Core.Interfaces.ServiceContracts;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;

namespace HR_System.Infrastructure.Services;

public class AccountService(UserManager<ApplicationUser> userManager,
    IApplicationUsersRepository usersRepository,
    ICookiesServices cookiesServices,
    ILogger<AccountService> logger,
    ITokensService tokensService) : IAccountService
{
    
    public async Task<Result<ApplicationUser>> CreateAccountAsync(
        UserCreateDTO toCreateData,
        string? imageUrl,
        string? imageId,
        CancellationToken cancellationToken = default)
    {
        if (toCreateData.Position == PositionsEnum.unknown)
        {
            logger.LogWarning("{serviceName}.{methodName} -- cannt add user with position of unknown",
                nameof(AccountService), nameof(CreateAccountAsync));
            return Result<ApplicationUser>.Failure("Cannt Add user with unknown position", HttpStatusCode.BadRequest);
        }
        
        // check if user already exists
        var doesUsesExist = await DoesUserExist(toCreateData, cancellationToken);
        if (doesUsesExist.IsSuccess)
        {
            logger.LogInformation("{serviceName}.{methodName} -- failed creating account because another user uses the same data\nErrors: {errors}",
                nameof(AccountService), nameof(CreateAccountAsync), doesUsesExist.Value);
            return Result<ApplicationUser>.Failure(doesUsesExist.Value!, HttpStatusCode.Conflict); // return fields used in other users
        }

        // Add user to DB
        var toAddUser = new ApplicationUser()
        {
            UserName = toCreateData.UserName,
            Email = toCreateData.Email,
            PhoneNumber = toCreateData.PhoneNumber,
            FullName = toCreateData.FullName,
            Position = toCreateData.Position,
            ImageUrl = imageUrl,
            PublicImageId = imageId
        };
        var createUserResult = await userManager.CreateAsync(toAddUser, toCreateData.Password);
        if (!createUserResult.Succeeded)
        {
            var errorsString = createUserResult.Errors.Select(e => e.Description);
            logger.LogError("{serviceName}.{methodName} -- failed adding user to database\nErrors: {errors}",
                nameof(AccountService), nameof(CreateAccountAsync), errorsString);
            return Result<ApplicationUser>.Failure(string.Join(" | ", errorsString));
        }
        
        // add user to his role
        var addUserToRoleResult = await userManager.AddToRoleAsync(toAddUser, toCreateData.Position == PositionsEnum.CEO ? 
            nameof(RolesEnum.Admin) : nameof(RolesEnum.Employee));

        if (!addUserToRoleResult.Succeeded)
        {
            var errorsString = addUserToRoleResult.Errors.Select(e => e.Description);
            logger.LogError("{serviceName}.{methodName} -- failed adding user to role\nErrors: {errors}",
                nameof(AccountService), nameof(CreateAccountAsync), errorsString);
            return Result<ApplicationUser>.Failure(string.Join(" | ", errorsString));
        }

        logger.LogInformation("{serviceName}.{methodName} -- User with UserId: {userId} username: {username} was created",
            nameof(AccountService), nameof(CreateAccountAsync), toAddUser.Id, toAddUser.UserName);
        
        return Result<ApplicationUser>.Success(toAddUser);
        
    }
    public async Task<Result<UserDTO>> LoginAsync(LoginDTO loginData, CancellationToken cancellationToken = default)
    {
        var user = await userManager.FindByEmailAsync(loginData.Email);
        if (user is null)
        {
            logger.LogWarning("{serviceName}.{methodName} -- failed to login because user doesnt exist",
                nameof(AccountService), nameof(LoginAsync));
            return Result<UserDTO>.Failure("Invalid Email or Password", HttpStatusCode.Unauthorized);
        }
        
        var isPasswordCorrect = await userManager.CheckPasswordAsync(user, loginData.Password);
        if (!isPasswordCorrect)
        {
            logger.LogWarning("{serviceName}.{methodName} -- failed to login for user {username} because wrong password",
                nameof(AccountService), nameof(LoginAsync), user.UserName);
            return Result<UserDTO>.Failure("Invalid Email or Password", HttpStatusCode.Unauthorized);
        }

        var generateTokensResult = await tokensService.GenerateNewTokensAsync(user, cancellationToken);
        if (!generateTokensResult.IsSuccess)
            return generateTokensResult.MapFailure<UserDTO>();

        var addTokensToCookiesResult = cookiesServices.AddTokens(generateTokensResult.Value!);
        if (!addTokensToCookiesResult.IsSuccess)
            return addTokensToCookiesResult.MapFailure<UserDTO>();
        
        logger.LogInformation("{serviceName}.{methodName} -- user with id {userId} successfully logged in",
            nameof(AccountService), nameof(LoginAsync), user.Id);
        
        return Result<UserDTO>.Success(user.ToUserDTO());
    }
    private async Task<Result<string>> DoesUserExist(UserCreateDTO toUserCreate, CancellationToken cancellationToken = default)
    {
        // check if user already Exists
        var existingUsers = await usersRepository.FilterAsync((u =>
                (u.UserName!.ToLower() == toUserCreate.UserName.ToLower() || 
                 u.Email!.ToLower() == toUserCreate.Email.ToLower() || 
                 u.PhoneNumber == toUserCreate.PhoneNumber)
            ),cancellationToken: cancellationToken);

        // if user already exist generate error message and return failure
        if (existingUsers.Any())
        {
            bool isEmailUsed = false , isPhoneUsed = false , isUserNameUsed = false;

            // see what is used
            foreach (var user in existingUsers)
            {
                if (user.UserName == toUserCreate.UserName) isUserNameUsed = true;
                if (user.Email == toUserCreate.Email) isEmailUsed = true;
                if (user.PhoneNumber == toUserCreate.PhoneNumber) isPhoneUsed = true;
                
                if(isEmailUsed && isPhoneUsed && isUserNameUsed) break;
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