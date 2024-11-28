namespace ITPE3200XAPI.DTOs.Post;

public class PostDto
{
    public string? PostId { get; set; }
    public string? Content { get; set; }
    public string? ProfilePicture { get; set; }
    public string? UserName { get; set; }
    public List<string>? ImageUrls { get; set; } // List of image URLs
    public bool IsLikedByCurrentUser { get; set; }
    public bool IsSavedByCurrentUser { get; set; }
    public bool IsOwnedByCurrentUser { get; set; }
    public int LikeCount { get; set; }
    public int CommentCount { get; set; }
    public List<CommentDto>? Comments { get; set; }
}