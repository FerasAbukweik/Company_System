using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Security.Claims;
using System.Text;
using HR_System.Core.Abstraction.ServiceContracts;
using HR_System.Core.Domain.Identity;
using HR_System.Core.DTO.Token;
using HR_System.Core.Options;
using HR_System.Core.Result;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace HR_System.Infrastructure.Services;

public class AccessTokenService(
    UserManager<ApplicationUser> userManager,
    IOptions<JWTOptions> jwtOptions) : IAccessTokenService
{
    public async Task<Result<AccessTokenResult>> GenerateAccessTokenAsync(ApplicationUser user)
    {
        // 1. Validate user inputs
        if (string.IsNullOrWhiteSpace(user.UserName) || string.IsNullOrWhiteSpace(user.Email))
        {
            return Result<AccessTokenResult>.Failure("Access token cannot be created because of missing UserName or Email.", HttpStatusCode.BadRequest);
        }

        // 2. Fetch roles
        var roles = await userManager.GetRolesAsync(user);
        if (!roles.Any())
        {
            return Result<AccessTokenResult>.Failure("User has no roles.", HttpStatusCode.BadRequest);
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
        var jwtKey = jwtOptions.Value.Key;
        if (string.IsNullOrEmpty(jwtKey))
        {
            return Result<AccessTokenResult>.Failure("Token configuration error.", HttpStatusCode.InternalServerError);
        }

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        // 5. Build token
        var token = new JwtSecurityToken(
            jwtOptions.Value.Issuer,
            jwtOptions.Value.Audience,
            claims,
            expires: DateTime.UtcNow.AddMinutes(jwtOptions.Value.AccessTokenLifeTime),
            signingCredentials: creds
        );

        var writtenToken = new JwtSecurityTokenHandler().WriteToken(token);


        return Result<AccessTokenResult>.Success(new AccessTokenResult()
        {
            AccessToken =  writtenToken,
            Expires = token.ValidTo,
        });
    }
}