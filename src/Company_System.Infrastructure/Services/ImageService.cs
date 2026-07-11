using CloudinaryDotNet;
using CloudinaryDotNet.Actions;
using HR_System.Core.Interfaces.ServiceContracts;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;

namespace HR_System.Infrastructure.Services;

public class ImageService : IImageService
{
    private readonly Cloudinary _cloudinary;
    public ImageService(IConfiguration configuration)
    {
        var account = new Account(
            configuration["CloudinarySettings:CloudName"], 
            configuration["CloudinarySettings:ApiKey"],
            configuration["CloudinarySettings:ApiSecret"]);
        
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
        
        return await _cloudinary.UploadAsync(uploadParams);
    }

    public async Task<DeletionResult> Delete(string globalId)
    {
        var deletionParams = new DeletionParams(globalId);

        return await _cloudinary.DestroyAsync(deletionParams);
    }
}