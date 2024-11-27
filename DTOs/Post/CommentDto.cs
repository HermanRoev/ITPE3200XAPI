namespace ITPE3200XAPI.DTOs;

public class CommentDto
{
    public string? CommentId { get; set; }
    public string? UserName { get; set; }
    public string? Content { get; set; }
    public DateTime CreatedAt { get; set; }
    public string? TimeSincePosted { get; set; }
    public bool IsCreatedByCurrentUser { get; set; }
}