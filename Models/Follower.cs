using System.ComponentModel.DataAnnotations;

namespace ITPE3200XAPI.Models
{
    public class Follower
    {
        [Required]
        [StringLength(36)]
        public string FollowerUserId { get; set; }

        [Required]
        [StringLength(36)]
        public string FollowedUserId { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Virtual navigation properties
        public virtual ApplicationUser FollowerUser { get; set; } = null!;
        public virtual ApplicationUser FollowedUser { get; set; } = null!;

        // Constructor to enforce required properties
        public Follower(string followerUserId, string followedUserId)
        {
            FollowerUserId = followerUserId ?? throw new ArgumentNullException(nameof(followerUserId));
            FollowedUserId = followedUserId ?? throw new ArgumentNullException(nameof(followedUserId));
        }
    }
}