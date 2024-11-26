using ITPE3200XAPI.Models;

namespace ITPE3200XAPI.ViewModels;

public class PostViewModel
{
        public string? PostId { get; set; }
        public string? Title { get; set; }
        public string? Content { get; set; }
        public string? ProfilePicture { get; set; }
        public string? UserName { get; set; }
        public ICollection<PostImage>? Images { get; set; }
        public bool IsLikedByCurrentUser { get; set; }
        public bool IsSavedByCurrentUser { get; set; }
        public bool IsOwnedByCurrentUser { get; set; }
        public bool HomeFeed { get; set; }
        public int LikeCount { get; set; }
        public int CommentCount { get; set; }
        public List<CommentViewModel>? Comments { get; set; }
}