using HR_System.ExtensionMethods;

namespace HR_System;

public static class WebApiDependencyInjectionExtension
{
    public static IServiceCollection AddWebApi(this IServiceCollection services, IConfiguration configuration)
    {
        services
            .AddJwtAuthentication()
            .AddCorsPolicies()
            .AddApiServices()
            .AddServiceDecorators()
            .AddJWTOptions(configuration)
            .AddCookieKeys(configuration)
            .AddCloudinaryOptions(configuration)
            .AddRedisOptions(configuration);

        return services;
    }
}
