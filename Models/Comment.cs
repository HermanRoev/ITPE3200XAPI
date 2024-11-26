using System.ComponentModel.DataAnnotations;

namespace ITPE3200XAPI.Models
{
    public class Comment
    {
        [Key]
        [StringLength(36, ErrorMessage = "CommentId cannot exceed 36 characters.")]
        public string CommentId { get; set; } = Guid.NewGuid().ToString();

        [Required]
        [StringLength(36, ErrorMessage = "PostId cannot exceed 36 characters.")]
        public string PostId { get; set; }

        [Required]
        [StringLength(36, ErrorMessage = "UserId cannot exceed 36 characters.")]
        public string UserId { get; set; }

        [Required]
        [StringLength(500, ErrorMessage = "Content cannot exceed 500 characters.")]
        public string Content { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Virtual navigation properties
        public virtual Post Post { get; set; } = null!;
        public virtual ApplicationUser User { get; set; } = null!;

        // Constructor to enforce required properties
        public Comment(string postId, string userId, string content)
        {
            PostId = postId ?? throw new ArgumentNullException(nameof(postId));
            UserId = userId ?? throw new ArgumentNullException(nameof(userId));
            Content = content ?? throw new ArgumentNullException(nameof(content));
        }
    }
}