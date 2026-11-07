using HR_System.Infrastructure.Decorators;
using HR_System.Core.Abstraction.RepositoryContracts;
using HR_System.Core.Abstraction.ServiceContracts;
using HR_System.Core.Domain.Identity;
using HR_System.Core.Options;
using HR_System.Infrastructure.BackGroundServices;
using HR_System.Infrastructure.Decorators.Caching;
using HR_System.Infrastructure.Decorators.Loggers;
using HR_System.Infrastructure.Repositories;
using HR_System.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.StackExchangeRedis;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using StackExchange.Redis;

namespace HR_System.Infrastructure.ExtensionMethods;

/// <summary>
/// The building blocks behind <c>AddInfrastructure</c>. They are public on purpose:
/// the only public entry point is <c>InfrastructureDependencyInjectionExtension.AddInfrastructure</c>.
/// </summary>
public static class InfrastructureServiceCollectionExtensions
{
    public static IServiceCollection AddSqlServer(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<ApplicationDbContext>(options =>
            options.UseSqlServer(configuration.GetConnectionString("Default")));

        return services;
    }

    public static IServiceCollection AddIdentityCore(this IServiceCollection services)
    {
        services.AddIdentityCore<ApplicationUser>(options =>
            {
                // user password attributes
                options.Password.RequiredLength = 8;
                options.Password.RequireLowercase = true;
                options.Password.RequireUppercase = true;
                options.Password.RequireDigit = true;
                options.Password.RequiredUniqueChars = 1;

                // lockout after repeated failed logins
                options.Lockout.AllowedForNewUsers = true;
                options.Lockout.MaxFailedAccessAttempts = 5;
                options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(5);
            })
            .AddRoles<ApplicationRole>()
            .AddEntityFrameworkStores<ApplicationDbContext>();

        return services;
    }

    public static IServiceCollection AddRedis(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddStackExchangeRedisCache(options =>
        {
            options.Configuration = configuration.GetConnectionString("Redis");
        });

        // instance name comes from RedisOptions (registered in the WebApi DI) so RedisService and the cache agree on it
        services.AddOptions<RedisCacheOptions>()
            .Configure<IOptions<RedisOptions>>((cache, redis) => cache.InstanceName = redis.Value.InstanceName);

        services.AddSingleton<IConnectionMultiplexer>(_ =>
            ConnectionMultiplexer.Connect(configuration.GetConnectionString("Redis") ?? "localhost:6379"));

        services.AddScoped<IRedisService, RedisService>();

        return services;
    }

    public static IServiceCollection AddServices(this IServiceCollection services)
    {
        services.AddScoped<ITokensService, TokensService>();
        services.AddScoped<IAccessTokenService, AccessTokenService>();
        services.AddScoped<IRefreshTokenService, RefreshTokenService>();
        services.AddScoped<ITasksService, TasksService>();
        services.AddScoped<IDailyWorkRecordsService, DailyWorkRecordsService>();
        services.AddScoped<IPaymentsService, PaymentsService>();
        services.AddScoped<IApprovalService, ApprovalService>();
        services.AddScoped<IActivitiesService, ActivitiesService>();
        services.AddScoped<IUsersService, UsersService>();
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IMessagesService, MessagesService>();
        services.AddScoped<INotificationsService, NotificationsService>();
        services.AddScoped<ITasksApprovalsService, TasksApprovalsService>();
        services.AddScoped<IImageService, ImageService>();
        services.AddScoped<IOrganizationHierarchyService, OrganizationHierarchyService>();

        return services;
    }

    public static IServiceCollection AddRepositories(this IServiceCollection services)
    {
        services.AddScoped<IActivityRepository, ActivityRepository>();
        services.AddScoped<IRefreshTokensRepository, RefreshTokensRepository>();
        services.AddScoped<IApplicationUsersRepository, ApplicationUsersRepository>();
        services.AddScoped<IApprovalRepository, ApprovalRepository>();
        services.AddScoped<ITasksRepository, TasksRepository>();
        services.AddScoped<IDailyWorkRecordsRepository, DailyWorkRecordsRepository>();
        services.AddScoped<IPaymentsRepository, PaymentsRepository>();
        services.AddScoped<IOrganizationHierarchyRepository, OrganizationHierarchyRepository>();
        services.AddScoped<IMessageRepository, MessageRepository>();
        services.AddScoped<INotificationsRepository, NotificationsRepository>();
        services.AddScoped<IImagesRepository, ImagesRepository>();

        return services;
    }

    /// <summary>
    /// Scrutor decorators. Each Decorate wraps whatever is registered at that point,
    /// so the LAST call for a service is its OUTERMOST layer.
    /// </summary>
    public static IServiceCollection AddServiceDecorators(this IServiceCollection services)
    {
        // redis: Logging -> RedisService
        services.Decorate<IRedisService, LoggingRedisService>();

        // services: Logging -> real service
        services.Decorate<ITokensService, LoggingTokensService>();
        services.Decorate<IAccessTokenService, LoggingAccessTokenService>();
        services.Decorate<IRefreshTokenService, LoggingRefreshTokenService>();
        services.Decorate<ITasksService, LoggingTasksService>();
        services.Decorate<IDailyWorkRecordsService, LoggingDailyWorkRecordsService>();
        services.Decorate<IPaymentsService, NotifyingPaymentsService>();
        services.Decorate<IPaymentsService, LoggingPaymentsService>();
        services.Decorate<IApprovalService, LoggingApprovalService>();
        services.Decorate<IActivitiesService, NotifyingActivitiesService>();
        services.Decorate<IActivitiesService, LoggingActivitiesService>();
        services.Decorate<IUsersService, InvalidatingUsersService>();
        services.Decorate<IUsersService, LoggingUsersService>();
        services.Decorate<IAuthService, LoggingAuthService>();
        services.Decorate<IMessagesService, LoggingMessagesService>();
        services.Decorate<INotificationsService, LoggingNotificationsService>();
        services.Decorate<ITasksApprovalsService, LoggingTasksApprovalsService>();
        services.Decorate<IImageService, LoggingImageService>();

        // organization hierarchy: Logging -> Cached -> OrganizationHierarchyService
        services.Decorate<IOrganizationHierarchyService, CachedOrganizationHierarchyService>();
        services.Decorate<IOrganizationHierarchyService, LoggingOrganizationHierarchyService>();

        return services;
    }

    public static IServiceCollection AddBackGroundServices(this IServiceCollection services)
    {
        services.AddHostedService<GenerateMonthlyPayments>();
        services.AddHostedService<RemoveExpiredRefreshTokens>();
        services.AddHostedService<RemoveOrphanedImages>();

        return services;
    }
}
