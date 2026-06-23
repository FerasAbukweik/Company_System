using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using HR_System.Core.Domain.Entities;
using HR_System.Core.Domain.Idnetity;
using HR_System.Core.Domain.RepositoryContracts;
using HR_System.Core.DTO;
using HR_System.Core.Helpers;
using HR_System.Core.ServiceContracts;
using HR_System.Core.ServiceContracts.ICurrentUserServices;
using HR_System.Core.ServiceContracts.ITokenServices;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;

namespace HR_System.Core.Services.TokenServices;

public class GenerateTokenService : IGenerateTokenService
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IConfiguration _configuration;
    private readonly IRefreshTokenRepository _refreshTokenRepository;
    private readonly IGetCurrentUserService _currentUserService;
    
    public GenerateTokenService(UserManager<ApplicationUser> userManager,
        IConfiguration configuration,
        IRefreshTokenRepository refreshTokenRepository,
        IGetCurrentUserService currentUserService)
    {
        _userManager = userManager;
        _configuration = configuration;
        _refreshTokenRepository = refreshTokenRepository;
        _currentUserService = currentUserService;
    }
    
    
    public async Task<Result<string>> GenerateAccessTokenAsync(ApplicationUser user)
    {
        if (string.IsNullOrWhiteSpace(user.UserName) || string.IsNullOrWhiteSpace(user.Email))
        {
            return Result<string>.Failure("access token Cannt be Created because of missing userName or Email" , HttpStatusCode.BadRequest);
        }
        
        var claims = new List<Claim>
        {
            new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new Claim(JwtRegisteredClaimNames.Iat, DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString()),

            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Name, user.UserName!),
            new Claim(ClaimTypes.Email, user.Email!),
        };
        
        var roles = await _userManager.GetRolesAsync(user);
        if (!roles.Any())
        {
            return Result<string>.Failure("user have no roles" , HttpStatusCode.BadRequest);
        }
        
        foreach (var role in roles)
        {
            claims.Add(new Claim(ClaimTypes.Role, role));
        }

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration.GetValue<string>("Jwt:Key")!));
        var creds = new SigningCredentials(key ,  SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            _configuration.GetValue<string>("Jwt:Issuer"),
            _configuration.GetValue<string>("Jwt:Audience"),
            claims,
            expires: DateTime.UtcNow.AddMinutes(_configuration.GetValue<int>("Jwt:AccessTokenLifeTime")),
            signingCredentials: creds
        );

        return Result<string>.Success(new JwtSecurityTokenHandler().WriteToken(token));
    }
    public async Task<Result<string>> GenerateRefreshTokenAsync(Guid userId , CancellationToken cancellationToken = default)
    {
        byte[] bytes = new byte[32];

        using (var rng = RandomNumberGenerator.Create())
        {
            rng.GetBytes(bytes);
        }
        string refreshToken = Convert.ToBase64String(bytes);

        
        // add refresh token to DB
        
        var toAddRefreshToken = new RefreshToken
        {
            Id = Guid.NewGuid(),
            Token = refreshToken,
            UserId = userId,
            Expires = DateTime.UtcNow.AddMinutes(_configuration.GetValue<int>("Jwt:RefreshTokenLifeTime")),
        };
        var addRefreshTokenRes = await _refreshTokenRepository.AddAsync(toAddRefreshToken , cancellationToken);
        if (!addRefreshTokenRes.IsSuccess) return addRefreshTokenRes.MapFailure<string>();
        
        return Result<string>.Success(refreshToken);
    }
    public async Task<Result<AccessAndRefreshTokenDTO>> GenerateNewAccessAndRefreshToken(ApplicationUser? user = null)
    {
        // check if user is null get curent user
        if (user == null)
        {
            var getCurrUserObjectResult = await _currentUserService.GetCurrUserObjectAsync();
            if(!getCurrUserObjectResult.IsSuccess) return getCurrUserObjectResult.MapFailure<AccessAndRefreshTokenDTO>();

            user = getCurrUserObjectResult.Value;
        }
        
        
        var generateAccessTokenResult = await this.GenerateAccessTokenAsync(user!);
        if (!generateAccessTokenResult.IsSuccess)
            return generateAccessTokenResult.MapFailure<AccessAndRefreshTokenDTO>();
        
        var generateRefreshTokenResult = await this.GenerateRefreshTokenAsync(user!.Id);
        if (!generateRefreshTokenResult.IsSuccess)
            return generateRefreshTokenResult.MapFailure<AccessAndRefreshTokenDTO>();

        var result = new AccessAndRefreshTokenDTO()
        {
            AccessToken = generateRefreshTokenResult.Value!,
            RefreshToken = generateRefreshTokenResult.Value!
        };

        return Result<AccessAndRefreshTokenDTO>.Success(result);
    }
}