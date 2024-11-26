using System.ComponentModel.DataAnnotations;

namespace ITPE3200XAPI.Models
{
    public class PostImage
    {
        [Key]
        [StringLength(36)]
        public string ImageId { get; set; } = Guid.NewGuid().ToString();

        [Required]
        [StringLength(36)]
        public string PostId { get; set; }

        [Required]
        [StringLength(256, ErrorMessage = "ImageUrl cannot exceed 256 characters.")]
        public string ImageUrl { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Virtual navigation property
        public virtual Post Post { get; set; } = null!;

        // Constructor to enforce required properties
        public PostImage(string postId, string imageUrl)
        {
            PostId = postId ?? throw new ArgumentNullException(nameof(postId));
            ImageUrl = imageUrl ?? throw new ArgumentNullException(nameof(imageUrl));
        }
    }
}