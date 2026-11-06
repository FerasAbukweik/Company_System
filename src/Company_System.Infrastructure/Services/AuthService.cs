using System.Net;
using HR_System.Core.Abstraction.RepositoryContracts;
using HR_System.Core.Abstraction.ServiceContracts;
using HR_System.Core.Domain.Identity;
using HR_System.Core.DTO.Auth;
using HR_System.Core.Result;
using Microsoft.AspNetCore.Identity;

namespace HR_System.Infrastructure.Services;

public class AuthService(
    UserManager<ApplicationUser> userManager,
    IApplicationUsersRepository usersRepository,
    ICookiesServices cookiesServices,
    ITokensService tokensService) : IAuthService
{
    public async Task<Result<UserDTO>> LoginAsync(LoginDTO loginData, CancellationToken cancellationToken = default)
    {
        // the image is loaded here because it is part of the returned UserDTO
        var user = await usersRepository.GetByEmailAsync(
            loginData.Email,
            [u => u.Image, u => u.OrganizationHierarchy],
            cancellationToken);
        if (user is null)
        {
            return Result<UserDTO>.Failure("Invalid Email or Password", HttpStatusCode.Unauthorized);
        }

        if (user.OrganizationHierarchy is { IsActive: false })
            return Result<UserDTO>.Failure("Account is inactive", HttpStatusCode.Forbidden);

        // checked before the password so a locked account can't keep guessing
        if (await userManager.IsLockedOutAsync(user))
        {
            return Result<UserDTO>.Failure("Account is locked. Try again later", HttpStatusCode.Locked);
        }

        var isPasswordCorrect = await userManager.CheckPasswordAsync(user, loginData.Password);
        if (!isPasswordCorrect)
        {
            // counts the failed attempt and locks the account once MaxFailedAccessAttempts is reached
            await userManager.AccessFailedAsync(user);
            return Result<UserDTO>.Failure("Invalid Email or Password", HttpStatusCode.Unauthorized);
        }

        // a successful login clears the failed-attempt counter
        await userManager.ResetAccessFailedCountAsync(user);

        var generateTokensResult = await tokensService.GenerateNewTokensAsync(user, cancellationToken);
        if (!generateTokensResult.IsSuccess)
            return generateTokensResult.MapFailure<UserDTO>();

        var addTokensToCookiesResult = cookiesServices.AddTokens(generateTokensResult.Value!);
        if (!addTokensToCookiesResult.IsSuccess)
            return addTokensToCookiesResult.MapFailure<UserDTO>();

        return Result<UserDTO>.Success(user.ToUserDTO());
    }

    public Result Logout()
    {
        // a missing cookie just means the user is already logged out, so the outcome is ignored
        cookiesServices.RemoveTokens();

        return Result.Success();
    }

    public async Task<Result> RefreshTokensAsync(CancellationToken cancellationToken = default)
    {
        // returned as a plain Result on purpose: the new tokens only go to the cookies, never to the response body
        Result result = await tokensService.UpdateUserTokensAsync(cancellationToken);

        return result;
    }
}
