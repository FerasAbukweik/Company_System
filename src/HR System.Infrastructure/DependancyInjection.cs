using HR_System.Core.Domain.RepositoryContract;
using HR_System.Infrastructure.Repositories;
using Microsoft.Extensions.DependencyInjection;

namespace HR_System.Infrastructure;

public static class RepositoryDependancyInjectionExtensionMethod
{
    public static IServiceCollection AddApplicationRepositories(this IServiceCollection services)
    {
        services.AddScoped<IRefreshTokenRepository, RefreshTokenRepository>();
        
        return services;
    }
}