using HR_System.Infrastructure.ExtensionMethods;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace HR_System.Infrastructure;

public static class InfrastructureDependencyInjectionExtension
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services
            .AddSqlServer(configuration)
            .AddIdentityCore()
            .AddRedis(configuration)
            .AddServices()
            .AddRepositories()
            .AddServiceDecorators()   // must stay after the registrations it wraps (Scrutor throws otherwise)
            .AddBackGroundServices();

        return services;
    }
}
