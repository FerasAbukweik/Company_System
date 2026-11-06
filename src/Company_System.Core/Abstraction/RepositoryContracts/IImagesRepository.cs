using System.Linq.Expressions;
using HR_System.Core.Domain.Entities;

namespace HR_System.Core.Abstraction.RepositoryContracts;

public interface IImagesRepository
{
    /// <summary>
    /// Gets an image by its id, without change tracking.
    /// </summary>
    /// <param name="id">The id of the image.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>The image, or null when it doesn't exist.</returns>
    Task<Image?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets an image by its id together with the given navigation properties, without change tracking.
    /// </summary>
    /// <param name="id">The id of the image.</param>
    /// <param name="include">Navigation properties to load together with the image.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>The image, or null when it doesn't exist.</returns>
    Task<Image?> GetByIdAsync(
        Guid id,
        Expression<Func<Image, object?>>[] include,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the images that no user owns and that were created before the given time, oldest first, without change tracking.
    /// </summary>
    /// <param name="createdBefore">Only images created at or before this time are returned.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>The orphaned images, or an empty list when there are none.</returns>
    Task<IReadOnlyList<Image>> GetOrphanedImagesAsync(DateTime createdBefore, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes the image rows straight in the DB, but only the ones that are still unowned
    /// (guards against an image being attached to a user meanwhile).
    /// </summary>
    /// <param name="imageIds">The ids of the images to delete.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>How many rows were deleted.</returns>
    Task<int> RemoveOrphanedImagesAsync(IReadOnlyCollection<Guid> imageIds, CancellationToken cancellationToken = default);
}
