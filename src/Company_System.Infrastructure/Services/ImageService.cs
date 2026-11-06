using CloudinaryDotNet;
using CloudinaryDotNet.Actions;
using HR_System.Core.Abstraction.ServiceContracts;
using HR_System.Core.DTO.Images;
using Microsoft.AspNetCore.Http;
using HR_System.Core.Options;
using Microsoft.Extensions.Options;

namespace HR_System.Infrastructure.Services;

public class ImageService: IImageService
{
    // Cloudinary's delete_resources accepts at most 100 public ids per request
    private const int MaxIdsPerRequest = 100;

    private readonly Cloudinary _cloudinary;
    public ImageService(IOptions<CloudinaryOptions> options)
    {
        var settings = options.Value;
        var account = new Account(
            settings.CloudName,
            settings.ApiKey,
            settings.ApiSecret);
        
        _cloudinary = new Cloudinary(account);
    }
    
    public async Task<ImageUploadResult> Upload(IFormFile image)
    {
        if (image.Length == 0) return new ImageUploadResult();
        
        await using var stream = image.OpenReadStream();
        var uploadParams = new ImageUploadParams()
        {
            File = new FileDescription(image.FileName, stream),
            Transformation = new Transformation().Height(500).Width(500).Crop("fill"),
            Folder = "Company_System"
        };
        
        var result = await _cloudinary.UploadAsync(uploadParams);
        return result;
    }

    public async Task<DeletionResult> Delete(string publicId)
    {
        var deletionParams = new DeletionParams(publicId);

        var result = await _cloudinary.DestroyAsync(deletionParams);
        return result;
    }

    public async Task<ImagesDeletionResult> DeleteMany(
        IReadOnlyCollection<string> publicIds,
        CancellationToken cancellationToken = default)
    {
        var gone = new List<string>();
        var failed = new List<string>();

        foreach (var batch in publicIds.Distinct().Chunk(MaxIdsPerRequest))
        {
            cancellationToken.ThrowIfCancellationRequested();

            var result = await _cloudinary.DeleteResourcesAsync(new DelResParams
            {
                PublicIds = batch.ToList(),
                ResourceType = ResourceType.Image
            });

            // whole request failed: nothing in this batch is known to be deleted
            if (result.Error != null || result.Deleted == null)
            {
                failed.AddRange(batch);
                continue;
            }

            foreach (var publicId in batch)
            {
                // "not_found" means it is already gone from Cloudinary
                var isGone = result.Deleted.TryGetValue(publicId, out var status)
                             && (status is "deleted" or "not_found");

                (isGone ? gone : failed).Add(publicId);
            }
        }

        return new ImagesDeletionResult(gone, failed);
    }
}
