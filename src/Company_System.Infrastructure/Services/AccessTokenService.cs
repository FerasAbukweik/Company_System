using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Security.Claims;
using System.Text;
using HR_System.Core.common;
using HR_System.Core.Domain.Identity;
using HR_System.Core.Interfaces.ServiceContracts;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;

namespace HR_System.Infrastructure.Services;

public class AccessTokenService(
    UserManager<ApplicationUser> userManager,
    IConfiguration configuration,
    ILogger<AccessTokenService> logger) : IAccessTokenService
{
    public async Task<Result<string>> GenerateAccessTokenAsync(ApplicationUser user)
    {
        // 1. Validate user inputs
        if (string.IsNullOrWhiteSpace(user.UserName) || string.IsNullOrWhiteSpace(user.Email))
        {
            logger.LogWarning("{ServiceName}.{MethodName} failed: Missing UserName or Email for User ID {UserId}.",
                nameof(AccessTokenService), nameof(GenerateAccessTokenAsync), user.Id);
                
            return Result<string>.Failure("Access token cannot be created because of missing UserName or Email.", HttpStatusCode.BadRequest);
        }

        // 2. Fetch roles
        var roles = await userManager.GetRolesAsync(user);
        if (!roles.Any())
        {
            logger.LogWarning("{ServiceName}.{MethodName} failed: User with ID {UserId} has no assigned roles.",
                nameof(AccessTokenService), nameof(GenerateAccessTokenAsync), user.Id);

            return Result<string>.Failure("User has no roles.", HttpStatusCode.BadRequest);
        }

        // 3. Setup claims
        var claims = new List<Claim>
        {
            new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new Claim(JwtRegisteredClaimNames.Iat, DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString(), ClaimValueTypes.Integer64),

            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Name, user.UserName),
            new Claim(ClaimTypes.Email, user.Email),
            new Claim("Position", user.Position.ToString())
        };

        claims.AddRange(roles.Select(role => new Claim(ClaimTypes.Role, role)));

        // 4. Generate security credentials
        var jwtKey = configuration.GetValue<string>("Jwt:Key");
        if (string.IsNullOrEmpty(jwtKey))
        {
            logger.LogError("{ServiceName}.{MethodName} failed: JWT signing key is not configured.",
                nameof(AccessTokenService), nameof(GenerateAccessTokenAsync));
            return Result<string>.Failure("Token configuration error.", HttpStatusCode.InternalServerError);
        }

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        // 5. Build token
        var token = new JwtSecurityToken(
            configuration.GetValue<string>("Jwt:Issuer"),
            configuration.GetValue<string>("Jwt:Audience"),
            claims,
            expires: DateTime.UtcNow.AddMinutes(configuration.GetValue<int>("Jwt:AccessTokenLifeTime")),
            signingCredentials: creds
        );

        var writtenToken = new JwtSecurityTokenHandler().WriteToken(token);

        logger.LogInformation("{ServiceName}.{MethodName} successfully generated access token for User ID {UserId}.",
            nameof(AccessTokenService), nameof(GenerateAccessTokenAsync), user.Id);

        return Result<string>.Success(writtenToken);
    }
}