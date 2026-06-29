using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using HR_System.Core.common;
using HR_System.Core.Constraints;
using HR_System.Core.Domain.Entities;
using HR_System.Core.Domain.Identity;
using HR_System.Core.DTO.Token;
using HR_System.Core.Interfaces.RepositoryContracts;
using HR_System.Core.Interfaces.ServiceContracts;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace HR_System.Infrastructure.Services;

public class TokenService(ICookiesServices cookiesServices,
    IRefreshTokensRepository refreshTokensRepository,
    UserManager<ApplicationUser> userManager,
    IConfiguration configuration,
    IOptions<CookieKeys> cookieKeys) : ITokenService
{
    public async Task<Result<string>> GenerateAccessTokenAsync(ApplicationUser user)
    {
        // if there is no email or userName return failure --because we need them later to create the token
        if (string.IsNullOrWhiteSpace(user.UserName) || string.IsNullOrWhiteSpace(user.Email))
        {
            return Result<string>.Failure("access token Cannt be Created because of missing userName or Email" , HttpStatusCode.BadRequest);
        }
        
        // claims
        var claims = new List<Claim>
        {
            new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new Claim(JwtRegisteredClaimNames.Iat, DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString()),

            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Name, user.UserName!),
            new Claim(ClaimTypes.Email, user.Email!),
        };
        
        var roles = await userManager.GetRolesAsync(user);
        if (!roles.Any())
            return Result<string>.Failure("user have no roles" , HttpStatusCode.BadRequest);
        
        // add roles to claims
        claims.AddRange(roles.Select(role => new Claim(ClaimTypes.Role, role)));

        // create SigningCredentials
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(configuration.GetValue<string>("Jwt:Key")!));
        var creds = new SigningCredentials(key ,  SecurityAlgorithms.HmacSha256);

        // generate token
        var token = new JwtSecurityToken(
            configuration.GetValue<string>("Jwt:Issuer"),
            configuration.GetValue<string>("Jwt:Audience"),
            claims,
            expires: DateTime.UtcNow.AddMinutes(configuration.GetValue<int>("Jwt:AccessTokenLifeTime")),
            signingCredentials: creds
        );

        return Result<string>.Success(new JwtSecurityTokenHandler().WriteToken(token));
    }
    public async Task<Result<string>> GenerateRefreshTokenAsync(Guid userId , CancellationToken cancellationToken = default)
    {
        // generate refresh token
        byte[] bytes = new byte[32];
        using (var rng = RandomNumberGenerator.Create())
        {
            rng.GetBytes(bytes);
        }
        string refreshToken = Convert.ToBase64String(bytes);

        
        // add refresh token to DB
        var toAddRefreshToken = new RefreshToken
        {
            Token = refreshToken,
            UserId = userId,
            Expires = DateTime.UtcNow.AddMinutes(configuration.GetValue<int>("Jwt:RefreshTokenLifeTime")),
        };
        refreshTokensRepository.AddAsync(toAddRefreshToken , cancellationToken);
        await refreshTokensRepository.SaveChangesAsync(cancellationToken);
        
        return Result<string>.Success(refreshToken);
    }
    public async Task<Result<AccessAndRefreshTokenDTO>> GenerateNewAccessAndRefreshTokenAsync(ApplicationUser user, CancellationToken cancellationToken = default)
    {
        // generate access token
        var generateAccessTokenResult = await this.GenerateAccessTokenAsync(user);
        if (!generateAccessTokenResult.IsSuccess)
            return generateAccessTokenResult.MapFailure<AccessAndRefreshTokenDTO>();
        
        // generate refresh token
        var generateRefreshTokenResult = await this.GenerateRefreshTokenAsync(user.Id, cancellationToken);
        if (!generateRefreshTokenResult.IsSuccess)
            return generateRefreshTokenResult.MapFailure<AccessAndRefreshTokenDTO>();

        // return result
        var result = new AccessAndRefreshTokenDTO()
        {
            AccessToken = generateRefreshTokenResult.Value!,
            RefreshToken = generateRefreshTokenResult.Value!
        };

        return Result<AccessAndRefreshTokenDTO>.Success(result);
    }
    
    public async Task<Result<RefreshToken>> IsRefreshTokenValid(Guid userId, CancellationToken cancellationToken = default)
    {
        // get refresh token from cookies
        var refreshTokenResult = cookiesServices.Get(cookieKeys.Value.RefreshToken);
        if (!refreshTokenResult.IsSuccess) 
            return refreshTokenResult.MapFailure<RefreshToken>(HttpStatusCode.Unauthorized);

        // get refresh token object 
        var refreshToken = await refreshTokensRepository
            .FindRefreshTokenByRefreshTokenStringAsync(refreshTokenResult.Value!, cancellationToken);
        if (refreshToken is null) 
            return Result<RefreshToken>.Failure("Refresh token not found", HttpStatusCode.Unauthorized);

        // if curr user isn't the owner of the refresh token return 401
        if (refreshToken.UserId != userId) 
            return Result<RefreshToken>.Failure("Invalid refresh token", HttpStatusCode.Unauthorized);

        // if refresh token is expired return 401
        if (refreshToken.IsResolved) 
            return Result<RefreshToken>.Failure("Invalid refresh token", HttpStatusCode.Unauthorized);

        return Result<RefreshToken>.Success(refreshToken);
    }
}