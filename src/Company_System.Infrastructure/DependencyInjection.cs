using HR_System.Core.Domain.Identity;
using HR_System.Core.Interfaces.RepositoryContracts;
using HR_System.Core.Interfaces.ServiceContracts;
using HR_System.Infrastructure.BackGroundServices;
using HR_System.Infrastructure.Repositories;
using HR_System.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using StackExchange.Redis;

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
        
        services.AddSingleton<IConnectionMultiplexer>(sp => 
            ConnectionMultiplexer.Connect(configuration.GetConnectionString("Redis") ??  "localhost:6379"));
        
        
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
        services.AddScoped<IRefreshTokensRepository, RefreshTokensRepository>();
        services.AddScoped<IApplicationUsersRepository, ApplicationUsersesRepository>();
        services.AddScoped<IApprovalRepository, ApprovalRepository>();
        services.AddScoped<ITasksRepository, TasksRepository>();
        services.AddScoped<IOrganizationHierarchyRepository, OrganizationHierarchyRepository>();
        services.AddScoped<IMessageRepository, MessageRepository>();
        
        // services
        services.AddScoped<ITokensService, TokensService>();
        services.AddScoped<IAccessTokenService, AccessTokenService>();
        services.AddScoped<IRefreshTokenService, RefreshTokenService>();
        services.AddScoped<ITasksService, TasksService>();
        services.AddKeyedScoped<IOrganizationHierarchyService, OrganizationHierarchyService>("inner");
        services.AddScoped<IOrganizationHierarchyService, CachedOrganizationHierarchyService>();
        services.AddScoped<IApprovalService, ApprovalService>();
        services.AddScoped<IActivitiesService, ActivitiesService>();
        services.AddScoped<IAccountService, AccountService>();
        services.AddScoped<IMessagesService, MessagesService>();
        services.AddScoped<ITasksApprovalsService, TasksApprovalsService>();
        services.AddScoped<IAccountOrgHierarchyService, AccountOrgHierarchyService>();
        services.AddScoped<IImageService, ImageService>();
        
        services.AddScoped<IRedisService, RedisService>();
        services.AddHostedRemoveExpiredRefreshTokens();
        
        
        return services;
    }
}