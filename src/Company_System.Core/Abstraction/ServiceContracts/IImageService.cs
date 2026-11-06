using CloudinaryDotNet.Actions;
using HR_System.Core.DTO.Images;
using Microsoft.AspNetCore.Http;

namespace HR_System.Core.Abstraction.ServiceContracts;

public interface IImageService
{
    /// <summary>
    /// Uploads an image to the image host, cropped to 500x500.
    /// </summary>
    /// <param name="image">The image file to upload.</param>
    /// <returns>The upload result of the host, or an empty result when the file has no content.</returns>
    Task<ImageUploadResult> Upload(IFormFile image);

    /// <summary>
    /// Deletes one image from the image host.
    /// </summary>
    /// <param name="publicId">The public id the host gave the image when it was uploaded.</param>
    /// <returns>The deletion result of the host.</returns>
    Task<DeletionResult> Delete(string publicId);

    /// <summary>
    /// Deletes many images with as few requests as the host allows (batches internally).
    /// An image that is already missing from the host counts as gone.
    /// </summary>
    /// <param name="publicIds">The public ids of the images to delete.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>Which public ids are gone and which failed to delete.</returns>
    Task<ImagesDeletionResult> DeleteMany(IReadOnlyCollection<string> publicIds, CancellationToken cancellationToken = default);
}
