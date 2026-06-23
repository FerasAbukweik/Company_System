using System.Net;
using HR_System.Core.Domain.Idnetity;
using HR_System.Core.Domain.RepositoryContracts;
using HR_System.Core.DTO;
using HR_System.Core.Helpers;
using HR_System.Core.ServiceContracts.IAccountServices;
using HR_System.Core.ServiceContracts.ICookieServices;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;

namespace HR_System.Core.Services.AccountServices;

public class CreateAccountService : ICreateAccountService
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IApplicationUserRepository _userRepository;
    private readonly RoleManager<ApplicationRole>  _roleManager;
    private readonly IAddCookieService _addCookieService;
    private readonly ILogger<CreateAccountService> _logger;

    public CreateAccountService(UserManager<ApplicationUser> userManager,
        IApplicationUserRepository userRepository,
        RoleManager<ApplicationRole>  roleManager,
        IAddCookieService addCookieService,
        ILogger<CreateAccountService> logger)
    {
        _userManager = userManager;
        _userRepository = userRepository;
        _roleManager = roleManager;
        _addCookieService = addCookieService;
        _logger = logger;
    }
    
    public async Task<Result<ApplicationUser>> CreateAccountAsync(CreateAccountDTO toCreate, CancellationToken cancellationToken = default)
    {
        // check if user already Exists
        #region checkIfAlreadyExist
        var getExistingUsersResult = await _userRepository.FilterAsync((u =>
                (u.UserName == toCreate.UserName || 
                 u.Email == toCreate.Email || 
                 u.PhoneNumber == toCreate.PhoneNumber)
            ),cancellationToken: cancellationToken);

        if (getExistingUsersResult.IsSuccess && getExistingUsersResult.Value!.Any())
        {
            bool isEmailUsed = false , isPhoneUsed = false , isUserNameUsed = false;

            foreach (var user in getExistingUsersResult.Value)
            {
                if (user.UserName == toCreate.UserName) isUserNameUsed = true;
                if (user.Email == toCreate.Email) isEmailUsed = true;
                if (user.PhoneNumber == toCreate.PhoneNumber) isPhoneUsed = true;
                
                if(isEmailUsed &&  isPhoneUsed && isUserNameUsed) break;
            }

            var usedFields = new List<string>();
            if (isEmailUsed) usedFields.Add($"Email '{toCreate.Email}'");
            if (isPhoneUsed) usedFields.Add($"Phone number '{toCreate.PhoneNumber}'");
            if (isUserNameUsed) usedFields.Add($"Username '{toCreate.UserName}'");

            string fieldsText = string.Join(", ", usedFields);
            string verb = usedFields.Count == 1 ? "is already used." : "are already used.";
            string errorMessage = $"{fieldsText} {verb}";

            return Result<ApplicationUser>.Failure(errorMessage.ToString() , HttpStatusCode.Conflict);
        }
        #endregion

        using var transaction = await _userRepository.BeginTransactionAsync(cancellationToken);
        
        // Add user to DB
        var toAddUser = new ApplicationUser()
        {
            Id = Guid.NewGuid(),
            UserName = toCreate.UserName,
            Email = toCreate.Email,
            PhoneNumber = toCreate.PhoneNumber,
            FullName = toCreate.FullName,
        };
        var createUserResult = await _userManager.CreateAsync(toAddUser, toCreate.Password);
        if (!createUserResult.Succeeded)
        {
            return Result<ApplicationUser>.Failure(string.Join(" | ", createUserResult.Errors.Select(e => e.Description)));
        }

        // if role doesnt exist add new row to the DB
        var doesRoleExist = await _roleManager.RoleExistsAsync(toCreate.Role.ToString());
        if (!doesRoleExist)
        {
            var toAddRole = new ApplicationRole()
            {
                Id = Guid.NewGuid(),
                Name = toCreate.Role.ToString()
            };

            var addRoleResult = await _roleManager.CreateAsync(toAddRole);
            if (!addRoleResult.Succeeded)
            {
                return Result<ApplicationUser>.Failure(string.Join(" | ", addRoleResult.Errors.Select(e => e.Description)));
            }
        }

        // add user to his role
        var addUserToRoleResult = await _userManager.AddToRoleAsync(toAddUser, toCreate.Role.ToString());
        if (!addUserToRoleResult.Succeeded)
        {
            return Result<ApplicationUser>.Failure(string.Join(" | ", addUserToRoleResult.Errors.Select(e => e.Description)));
        }

        // generate new Tokens and store them in cookies
        var generateAndStoreTokensResult = await _addCookieService.AddTokensToCookies();
        if (!generateAndStoreTokensResult.IsSuccess)
        {
            _logger.LogError($"Failed to generate or store tokens in cookies\nError: {generateAndStoreTokensResult.ErrorMessage}");
        }

        await transaction.CommitAsync(cancellationToken);
        return Result<ApplicationUser>.Success(toAddUser);
    }
}