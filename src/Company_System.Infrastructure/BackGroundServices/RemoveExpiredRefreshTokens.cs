using HR_System.Core.Interfaces.RepositoryContracts;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace HR_System.Infrastructure.BackGroundServices;

public class RemoveExpiredRefreshTokens(IServiceScopeFactory serviceScopeFactory) : BackgroundService
{
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
        using var scope = serviceScopeFactory.CreateScope();
        
        // get RefreshTokenRepository
        var refreshTokenRepository = scope.ServiceProvider.GetRequiredService<IRefreshTokenRepository>();

        // renove expired refresh tokens
        await refreshTokenRepository.RemoveExpiredRefreshTokensAsync(stoppingToken);
    }
}

// add extension method for this BG service 
public static class AddRemoveExpiredRefreshTokensExtensionMethod
{
    public static IServiceCollection AddHostedRemoveExpiredRefreshTokens(this IServiceCollection services)
    {
        return services.AddHostedService<RemoveExpiredRefreshTokens>();
    }
}