using System.Net;
using HR_System.Core.common;
using HR_System.Core.Domain.Idnetity;
using HR_System.Core.DTO;
using HR_System.Core.DTO.Auth;
using HR_System.Core.DTO.Token;
using HR_System.Core.Interfaces.RepositoryContracts;
using HR_System.Core.Interfaces.ServiceContracts.IAccountServices;
using HR_System.Core.Interfaces.ServiceContracts.ICookieServices;
using HR_System.Core.Interfaces.ServiceContracts.ITokenServices;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;

namespace HR_System.Infrastructure.Services;

public class AccountService(UserManager<ApplicationUser> userManager,
    IApplicationUserRepository userRepository,
    ICookieService cookieService,
    ILogger<AccountService> logger,
    ITokenService tokenService) : IAccountService
{
    
    public async Task<Result<ApplicationUser>> CreateAccountAsync(CreateAccountDTO toCreate, CancellationToken cancellationToken = default)
    {
        // check if user already exists
        var doesUsesExist = await DoesUserExist(toCreate, cancellationToken);
        if (doesUsesExist.IsSuccess)
            return Result<ApplicationUser>.Failure(doesUsesExist.Value!, HttpStatusCode.Conflict); // return fields used in other users

        // use dbContext transaction so we Roll back in case something went wrong 
        using var transaction = await userRepository.BeginTransactionAsync(cancellationToken);
        
        // Add user to DB
        var toAddUser = new ApplicationUser()
        {
            UserName = toCreate.UserName,
            Email = toCreate.Email,
            PhoneNumber = toCreate.PhoneNumber,
            FullName = toCreate.FullName,
        };
        var createUserResult = await userManager.CreateAsync(toAddUser, toCreate.Password);
        if (!createUserResult.Succeeded)
            return Result<ApplicationUser>.Failure(string.Join(" | ", createUserResult.Errors.Select(e => e.Description)));
        
        // add user to his role
        var addUserToRoleResult = await userManager.AddToRoleAsync(toAddUser, toCreate.Role.ToString());
        if (!addUserToRoleResult.Succeeded)
            return Result<ApplicationUser>.Failure(string.Join(" | ", addUserToRoleResult.Errors.Select(e => e.Description)));
        
        // generate new Tokens
        var generateTokensResult = await tokenService.GenerateNewAccessAndRefreshTokenAsync(toAddUser, cancellationToken);
        if(!generateTokensResult.IsSuccess)
            return generateTokensResult.MapFailure<ApplicationUser>();
        
        // add tokens to cookies
        cookieService.AddTokens(generateTokensResult.Value!);

        // apply changes to DB
        await transaction.CommitAsync(cancellationToken);
        
        logger.LogInformation($"User Created At: {DateTime.UtcNow}\n\nUser:\n{toAddUser.ToString()}");
        
        return Result<ApplicationUser>.Success(toAddUser);
    }
    private async Task<Result<string>> DoesUserExist(CreateAccountDTO toCreate, CancellationToken cancellationToken = default)
    {
        // check if user already Exists
        var existingUsers = await userRepository.FilterAsync((u =>
                (u!.UserName!.ToLower() == toCreate.UserName.ToLower() || 
                 u!.Email!.ToLower() == toCreate.Email.ToLower() || 
                 u.PhoneNumber == toCreate.PhoneNumber)
            ),cancellationToken: cancellationToken);

        // if user already exist generate error message and return failure
        if (existingUsers.Any())
        {
            bool isEmailUsed = false , isPhoneUsed = false , isUserNameUsed = false;

            // see what is used
            foreach (var user in existingUsers)
            {
                if (user.UserName == toCreate.UserName) isUserNameUsed = true;
                if (user.Email == toCreate.Email) isEmailUsed = true;
                if (user.PhoneNumber == toCreate.PhoneNumber) isPhoneUsed = true;
                
                if(isEmailUsed && isPhoneUsed && isUserNameUsed) break;
            }
 
            // collect used fields in list
            var usedFields = new List<string>();
            if (isEmailUsed) usedFields.Add($"Email '{toCreate.Email}'");
            if (isPhoneUsed) usedFields.Add($"Phone number '{toCreate.PhoneNumber}'");
            if (isUserNameUsed) usedFields.Add($"Username '{toCreate.UserName}'");

            // generate error message
            string fieldsText = string.Join(", ", usedFields);
            string verb = usedFields.Count == 1 ? "is already used." : "are already used.";
            string errorMessage = $"{fieldsText} {verb}";

            return Result<string>.Success(errorMessage);
        }

        return Result<string>.Failure("");
    }
    
    public async Task<Result<AccessAndRefreshTokenDTO>> LoginAsync(LoginDTO loginData, CancellationToken cancellationToken = default)
    {
        var user = await userManager.FindByEmailAsync(loginData.Email);
        if(user is null)
            return Result<AccessAndRefreshTokenDTO>.Failure("Invalid Email Or Password", HttpStatusCode.Unauthorized);
        
        var isPasswordCorrect = await userManager.CheckPasswordAsync(user, loginData.Password);
        if(!isPasswordCorrect)
            return Result<AccessAndRefreshTokenDTO>.Failure("Invalid Email Or Password", HttpStatusCode.Unauthorized);

        var generateTokensResult = await tokenService.GenerateNewAccessAndRefreshTokenAsync(user, cancellationToken);
        if (!generateTokensResult.IsSuccess)
            return generateTokensResult;

        var addTokensToCookiesResult = cookieService.AddTokens(generateTokensResult.Value!);
        if (!addTokensToCookiesResult.IsSuccess) return addTokensToCookiesResult.MapFailure<AccessAndRefreshTokenDTO>();

        return Result<AccessAndRefreshTokenDTO>.Success(generateTokensResult.Value!);
    }
}