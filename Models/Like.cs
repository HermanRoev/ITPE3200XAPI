using System.ComponentModel.DataAnnotations;

namespace ITPE3200XAPI.Models
{
    public class Like
    {
        [Required]
        [StringLength(36)]
        public string PostId { get; set; }

        [Required]
        [StringLength(36)]
        public string UserId { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Virtual navigation properties
        public virtual Post Post { get; set; } = null!;
        public virtual ApplicationUser User { get; set; } = null!;

        // Constructor to enforce required properties
        public Like(string postId, string userId)
        {
            PostId = postId ?? throw new ArgumentNullException(nameof(postId));
            UserId = userId ?? throw new ArgumentNullException(nameof(userId));
        }
    }
}