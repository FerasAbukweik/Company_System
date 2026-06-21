using HR_System.Core.ServiceContracts;
using HR_System.Core.Services.TokenServices;
using Microsoft.Extensions.DependencyInjection;

namespace HR_System.Core.ExtensionMethods;

public static class ServicesDependancyInjectionExtensionMethod
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        // refreshToken Services
        services.AddScoped<IGenerateTokenService, GenerateTokenService>();

        return services;
    }
}