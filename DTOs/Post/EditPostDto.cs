namespace ITPE3200XAPI.DTOs.Post;

public class EditPostDto
{
    public string? PostId { get; set; }
    public string? Content { get; set; }
    public List<IFormFile>? ImageFiles { get; set; }
}