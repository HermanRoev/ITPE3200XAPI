namespace ITPE3200XAPI.ViewModels;

public class CommentViewModel
{
    public bool IsCreatedByCurrentUser { get; set; }
    public string? CommentId { get; set; }
    public string? UserName { get; set; }
    public string? Content { get; set; }
    public DateTime CreatedAt { get; set; }
    public string? TimeSincePosted { get; set; }
}