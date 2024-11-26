using System.ComponentModel.DataAnnotations;

namespace ITPE3200XAPI.Models
{
    public class Post
    {
        [Key]
        [StringLength(36)]
        public string PostId { get; set; } = Guid.NewGuid().ToString();

        [Required]
        [StringLength(36)]
        public string UserId { get; set; }

        [Required]
        [StringLength(2000, ErrorMessage = "Content cannot exceed 2000 characters.")]
        public string Content { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Virtual navigation properties
        public virtual ApplicationUser User { get; set; } = null!;
        public virtual ICollection<Comment> Comments { get; set; } = new HashSet<Comment>();
        public virtual ICollection<Like> Likes { get; set; } = new HashSet<Like>();
        public virtual ICollection<SavedPost> SavedPosts { get; set; } = new HashSet<SavedPost>();
        public virtual ICollection<PostImage> Images { get; set; } = new HashSet<PostImage>();

        // Constructor to enforce required properties
        public Post(string userId, string content)
        {
            UserId = userId ?? throw new ArgumentNullException(nameof(userId));
            Content = content ?? throw new ArgumentNullException(nameof(content));
        }
    }
}