using System.Text;
using HR_System.Core.Constraints;
using HR_System.Core.Interfaces.ServiceContracts;
using HR_System.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace HR_System;

public static class WebApiDependencyInjectionExtensionMethod
{
    public static IServiceCollection AddWebApi(this IServiceCollection services, IConfiguration configuration)
    { 
        // Add jwt bearer
        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                // validation parameters
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidIssuer = configuration.GetValue<string>("Jwt:Issuer"),
                    ValidateAudience = true,
                    ValidAudience = configuration.GetValue<string>("Jwt:Audience"),
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(configuration.GetValue<string>("Jwt:Key")!)),
                    ValidateLifetime = true
                };

                options.Events = new JwtBearerEvents
                {
                    // Get Token From Cookies
                    OnMessageReceived = context =>
                    {
                        var cookieKeys = context.HttpContext.RequestServices
                            .GetRequiredService<IOptions<CookieKeys>>();
                        
                        if (context.HttpContext.Request.Cookies.TryGetValue(cookieKeys.Value.AccessToken, out var accessToken))
                        {
                            context.Token = accessToken;
                        }
                
                        return  Task.CompletedTask;
                    }
                };
            });
        
        
        services.AddCors(options =>
        {
            options.AddPolicy("Angular", policy =>
            {
                policy.WithOrigins("http://localhost:4200")
                    .AllowAnyHeader()
                    .AllowAnyMethod()
                    .AllowCredentials();
            });
        });

        
        services.AddControllers();
        services.AddOpenApi();
        services.AddHttpContextAccessor();
        services.AddScoped<ICookiesServices, CookiesServices>();
        services.AddSignalR(op => op.EnableDetailedErrors = true);
        services.Configure<CookieKeys>(configuration.GetSection("CookieKeys"));
        
        return services;
    }
}