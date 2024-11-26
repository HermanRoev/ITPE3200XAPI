using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;

namespace ITPE3200XAPI.Models
{
    public class ApplicationUser : IdentityUser
    {
        [StringLength(256, ErrorMessage = "Max length for URL is 256 characters.")]
        public string? ProfilePictureUrl { get; set; }

        [StringLength(2000, ErrorMessage = "Max length for Bio is 2000 characters.")]
        public string? Bio { get; set; }

        // Virtual navigation properties for lazy loading
        public virtual ICollection<Post> Posts { get; set; } = new HashSet<Post>();
        public virtual ICollection<Comment> Comments { get; set; } = new HashSet<Comment>();
        public virtual ICollection<Like> Likes { get; set; } = new HashSet<Like>();
        public virtual ICollection<SavedPost> SavedPosts { get; set; } = new HashSet<SavedPost>();
        public virtual ICollection<Follower> Followers { get; set; } = new HashSet<Follower>();
        public virtual ICollection<Follower> Following { get; set; } = new HashSet<Follower>();
    }
}