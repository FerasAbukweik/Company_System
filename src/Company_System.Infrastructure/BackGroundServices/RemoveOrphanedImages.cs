using HR_System.Core.Abstraction.RepositoryContracts;
using HR_System.Core.Abstraction.ServiceContracts;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace HR_System.Infrastructure.BackGroundServices;

/// <summary>
/// Removes images that have no user (UserId is null) from Cloudinary and then from the database.
/// </summary>
public class RemoveOrphanedImages(
    IServiceScopeFactory serviceScopeFactory,
    ILogger<RemoveOrphanedImages> logger) : BackgroundService
{
    private static readonly TimeSpan Interval = TimeSpan.FromDays(1);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await RemoveOrphanedImagesMethod(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                // one failed run must never kill the service
                logger.LogError(ex, "{ServiceName} run failed", nameof(RemoveOrphanedImages));
            }

            try
            {
                await Task.Delay(Interval, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
        }
    }

    private async Task RemoveOrphanedImagesMethod(CancellationToken stoppingToken)
    {
        using var scope = serviceScopeFactory.CreateScope();
        var imagesRepository = scope.ServiceProvider.GetRequiredService<IImagesRepository>();
        var imageService = scope.ServiceProvider.GetRequiredService<IImageService>();

        var orphans = await imagesRepository.GetOrphanedImagesAsync(
            DateTime.UtcNow.AddHours(-1), stoppingToken);

        if (orphans.Count == 0) return;

        // Cloudinary first: rows whose file could not be deleted stay and the next run retries them
        var deletionResult = await imageService.DeleteMany(
            orphans.Select(i => i.PublicId).ToList(), stoppingToken);

        // "gone" includes files that were already missing from Cloudinary, so their rows can go too
        var gone = deletionResult.Gone.ToHashSet();
        var goneImageIds = orphans
            .Where(i => gone.Contains(i.PublicId))
            .Select(i => i.Id)
            .ToList();

        var removedCount = goneImageIds.Count == 0
            ? 0
            : await imagesRepository.RemoveOrphanedImagesAsync(goneImageIds, stoppingToken);

        logger.LogInformation("{ServiceName} removed {Count} of {Total} orphaned images",
            nameof(RemoveOrphanedImages), removedCount, orphans.Count);
    }
}
