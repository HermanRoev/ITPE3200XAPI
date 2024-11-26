using ITPE3200XAPI.Models;

namespace ITPE3200XAPI.ViewModels;

public class EditPostViewModel
{
    public ICollection<PostImage>? Images { get; set; }
    public string? Content { get; set; }
    public string? PostId { get; set; }

}