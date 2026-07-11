using CloudinaryDotNet.Actions;
using Microsoft.AspNetCore.Http;

namespace HR_System.Core.Interfaces.ServiceContracts;

public interface IImageService
{
    Task<ImageUploadResult> Upload(IFormFile image);
    Task<DeletionResult> Delete(string globalId);
}