namespace ITPE3200XAPI.DTOs.Post;
    
public class CreatePostDto
{
    public String? Content { get; set; }
    public List<IFormFile>? ImageFiles { get; set; }
}