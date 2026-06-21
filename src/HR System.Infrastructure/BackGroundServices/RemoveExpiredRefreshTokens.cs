using HR_System.Core.Domain.RepositoryContract;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace HR_System.Infrastructure.BackGroundServices;

public class RemoveExpiredRefreshTokens : BackgroundService
{
    private readonly IServiceScopeFactory _serviceScopeFactory;
    
    public RemoveExpiredRefreshTokens(IServiceScopeFactory serviceScopeFactory)
    {
        _serviceScopeFactory = serviceScopeFactory;
    }
    
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            await RemoveExpiredRefreshTokensMethod(stoppingToken);
            
            await Task.Delay(TimeSpan.FromDays(1), stoppingToken);
        }
    }

    private async Task RemoveExpiredRefreshTokensMethod(CancellationToken stoppingToken)
    {
        using (var scope = _serviceScopeFactory.CreateScope())
        {
            var refreshTokenRepository = scope.ServiceProvider.GetRequiredService<IRefreshTokenRepository>();

            await refreshTokenRepository.RemoveExpiredRefreshTokensAsync(stoppingToken);
        }
    }
}

public static class AddRemoveExpiredRefreshTokensExtensionMethod
{
    public static IServiceCollection AddHostedRemoveExpiredRefreshTokens(this IServiceCollection services)
    {
        return services.AddHostedService<RemoveExpiredRefreshTokens>();
    }
}