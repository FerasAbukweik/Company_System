using HR_System.Core.Domain.Identity;
using HR_System.Core.Interfaces.RepositoryContracts;
using HR_System.Core.Interfaces.ServiceContracts.IAccountServices;
using HR_System.Core.Interfaces.ServiceContracts.IActivitiesService;
using HR_System.Core.Interfaces.ServiceContracts.IApprovalService;
using HR_System.Core.Interfaces.ServiceContracts.IMessageServices;
using HR_System.Core.Interfaces.ServiceContracts.IOrganizationHierarchyService;
using HR_System.Core.Interfaces.ServiceContracts.IRedisService;
using HR_System.Core.Interfaces.ServiceContracts.ITaskServices;
using HR_System.Core.Interfaces.ServiceContracts.ITokenServices;
using HR_System.Infrastructure.BackGroundServices;
using HR_System.Infrastructure.Repositories;
using HR_System.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace HR_System.Infrastructure;

public static class InfrastructureDependencyInjectionExtensionMethod
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        // add ApplicationDbContext to services
        services.AddDbContext<ApplicationDbContext>(options => options.UseSqlServer(configuration.GetConnectionString("Default")));

        // add redis
        services.AddStackExchangeRedisCache(options =>
        {
            options.Configuration = configuration.GetConnectionString("Redis");
            options.InstanceName = configuration["Redis:InstanceName"];
        });
        
        
        // add identity
        services.AddIdentityCore<ApplicationUser>(options =>
            {
                // user password attributes
                options.Password.RequiredLength = 8;
                options.Password.RequireLowercase = true;
                options.Password.RequireUppercase = true;
                options.Password.RequireDigit = true;
                options.Password.RequiredUniqueChars = 1;
            })
            .AddRoles<ApplicationRole>()
            .AddEntityFrameworkStores<ApplicationDbContext>();

        // repositories
        services.AddScoped<IActivityRepository, ActivityRepository>();
        services.AddScoped<IRefreshTokensRepository, RefreshTokensesRepository>();
        services.AddScoped<IApplicationUsersRepository, ApplicationUsersesRepository>();
        services.AddScoped<IApprovalRepository, ApprovalRepository>();
        services.AddScoped<ITasksRepository, TasksRepository>();
        services.AddScoped<IOrganizationHierarchyRepository, OrganizationHierarchyRepository>();
        services.AddScoped<IMessageRepository, MessageRepository>();
        
        // services
        services.AddScoped<ITokenService, TokenService>();
        services.AddScoped<ITasksService, TasksService>();
        services.AddScoped<IOrganizationHierarchyService, OrganizationHierarchyService>();
        services.AddScoped<IApprovalService, ApprovalService>();
        services.AddScoped<IActivitiesService, ActivitiesService>();
        services.AddScoped<IAccountService, AccountService>();
        services.AddScoped<IMessageService, MessageService>();
        
        services.AddScoped<IRedisService, RedisService>();
        services.AddHostedRemoveExpiredRefreshTokens();
        
        
        return services;
    }
}