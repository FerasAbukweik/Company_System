using CloudinaryDotNet.Actions;
using HR_System.Core.Abstraction.ServiceContracts;
using HR_System.Core.DTO.Images;
using HR_System.Infrastructure.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace HR_System.Infrastructure.Decorators.Loggers;

public class LoggingImageService(
    IImageService inner,
    ILogger<LoggingImageService> logger) : IImageService
{
    private const string ServiceName = nameof(ImageService);

    public async Task<ImageUploadResult> Upload(IFormFile image)
    {
        var result = await inner.Upload(image);

        if (result.Error != null)
        {
            logger.LogError("{ServiceName}.{MethodName} failed uploading image. Error: {Error}",
                ServiceName, nameof(Upload), result.Error.Message);
        }
        else
        {
            logger.LogInformation("{ServiceName}.{MethodName} image was uploaded with publicId of {PublicId}",
                ServiceName, nameof(Upload), result.PublicId);
        }

        return result;
    }

    public async Task<DeletionResult> Delete(string publicId)
    {
        var result = await inner.Delete(publicId);

        if (result.Error != null)
        {
            logger.LogError("{ServiceName}.{MethodName} failed deleting image with publicId of {PublicId}. Error: {Error}",
                ServiceName, nameof(Delete), publicId, result.Error.Message);
        }
        else
        {
            logger.LogInformation("{ServiceName}.{MethodName} image with publicId of {PublicId} was deleted",
                ServiceName, nameof(Delete), publicId);
        }

        return result;
    }

    public async Task<ImagesDeletionResult> DeleteMany(
        IReadOnlyCollection<string> publicIds,
        CancellationToken cancellationToken = default)
    {
        var result = await inner.DeleteMany(publicIds, cancellationToken);

        if (result.Failed.Count > 0)
        {
            logger.LogError("{ServiceName}.{MethodName} failed deleting {FailedCount} of {Total} images. PublicIds: {PublicIds}",
                ServiceName, nameof(DeleteMany), result.Failed.Count, publicIds.Count, string.Join(", ", result.Failed));
        }

        logger.LogInformation("{ServiceName}.{MethodName} {GoneCount} of {Total} images are gone from the host",
            ServiceName, nameof(DeleteMany), result.Gone.Count, publicIds.Count);

        return result;
    }
}
