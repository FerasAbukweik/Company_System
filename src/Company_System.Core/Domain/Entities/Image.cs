using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using HR_System.Core.Domain.Identity;

namespace HR_System.Core.Domain.Entities;

public class Image
{
    public Guid Id { get; set; } = Guid.NewGuid();

    // Cloudinary public id, needed to delete the file from Cloudinary
    [Required]
    [Column(TypeName = "nvarchar(150)")]
    public required string PublicId { get; set; }

    [Required]
    [Column(TypeName = "nvarchar(500)")]
    public required string Url { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;


    // relations (one to one, the FK lives on the image)
    // null => nobody owns this image, the background service will remove it from Cloudinary
    public Guid? UserId { get; set; }
    public ApplicationUser? User { get; set; }


    // override
    public override string ToString()
    {
        return $"Id: {Id}\nPublicId: {PublicId}\nUserId: {UserId}\nUrl: {Url}\n";
    }

    public override bool Equals(object? obj)
    {
        if (obj is not Image other)
            return false;

        return other.Id == Id &&
               other.PublicId == PublicId &&
               other.UserId == UserId;
    }

    public override int GetHashCode() => Id.GetHashCode();
}
