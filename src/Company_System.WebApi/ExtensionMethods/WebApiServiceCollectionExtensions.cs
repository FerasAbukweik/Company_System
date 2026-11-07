using System.Text;
using System.Security.Claims;
using HR_System.Core.Abstraction.RepositoryContracts;
using HR_System.Core.Abstraction.ServiceContracts;
using HR_System.Core.Options;
using HR_System.Services;
using HR_System.Services.Loggers;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace HR_System.ExtensionMethods;

/// <summary>
/// The building blocks behind <c>AddWebApi</c>. They are public on purpose:
/// the only public entry point is <c>WebApiDependencyInjectionExtension.AddWebApi</c>.
/// </summary>
public static class WebApiServiceCollectionExtensions
{
    public static IServiceCollection AddJwtAuthentication(this IServiceCollection services)
    {
        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer();

        // configured through options so the values come from JWTOptions / CookieKeysOptions, not raw IConfiguration
        services.AddOptions<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme)
            .Configure<IOptions<JWTOptions>, IOptions<CookieKeysOptions>>((options, jwtOptions, cookieKeys) =>
            {
                var jwt = jwtOptions.Value;

                // validation parameters
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidIssuer = jwt.Issuer,
                    ValidateAudience = true,
                    ValidAudience = jwt.Audience,
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt.Key)),
                    ValidateLifetime = true
                };

                options.Events = new JwtBearerEvents
                {
                    // Get Token From Cookies
                    OnMessageReceived = context =>
                    {
                        if (context.HttpContext.Request.Cookies.TryGetValue(cookieKeys.Value.AccessToken, out var accessToken))
                        {
                            context.Token = accessToken;
                        }

                        return Task.CompletedTask;
                    },
                    OnTokenValidated = async context =>
                    {
                        var value = context.Principal?.FindFirstValue(ClaimTypes.NameIdentifier);
                        if (!Guid.TryParse(value, out var userId)) { context.Fail("Invalid user"); return; }
                        var users = context.HttpContext.RequestServices.GetRequiredService<IApplicationUsersRepository>();
                        var user = await users.GetByIdAsync(userId, [u => u.OrganizationHierarchy], context.HttpContext.RequestAborted);
                        if (user is null || user.OrganizationHierarchy is { IsActive: false }) context.Fail("Account is inactive");
                    }
                };
            });

        return services;
    }

    public static IServiceCollection AddCorsPolicies(this IServiceCollection services)
    {
        services.AddCors(options =>
        {
            options.AddPolicy("Angular", policy =>
            {
                policy.WithOrigins("https://localhost:4200", "https://localhost:4000", "http://localhost:4200", "http://localhost:4000")
                    .AllowAnyHeader()
                    .AllowAnyMethod()
                    .AllowCredentials();
            });
        });

        return services;
    }

    public static IServiceCollection AddApiServices(this IServiceCollection services)
    {
        services.AddControllers();
        services.AddOpenApi();
        services.AddHttpContextAccessor();
        services.AddSignalR(op => op.EnableDetailedErrors = true);

        services.AddScoped<ICookiesServices, CookiesServices>();
        services.AddScoped<IClaimsService, ClaimsService>();
        services.AddScoped<NotificationPublisher>();
        services.AddScoped<INotificationPublisher>(provider => provider.GetRequiredService<NotificationPublisher>());

        return services;
    }

    /// <summary>
    /// Scrutor decorators. Each Decorate wraps whatever is registered at that point,
    /// so the LAST call for a service is its OUTERMOST layer.
    /// </summary>
    public static IServiceCollection AddServiceDecorators(this IServiceCollection services)
    {
        // cookies: Logging -> CookiesServices
        services.Decorate<ICookiesServices, LoggingCookiesServices>();

        return services;
    }

    public static IServiceCollection AddJWTOptions(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<JWTOptions>(configuration.GetSection(JWTOptions.SectionName));

        return services;
    }

    public static IServiceCollection AddCookieKeys(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<CookieKeysOptions>(configuration.GetSection(CookieKeysOptions.SectionName));

        return services;
    }

    public static IServiceCollection AddCloudinaryOptions(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<CloudinaryOptions>(configuration.GetSection(CloudinaryOptions.SectionName));

        return services;
    }

    public static IServiceCollection AddRedisOptions(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<RedisOptions>(configuration.GetSection(RedisOptions.SectionName));

        return services;
    }
}
