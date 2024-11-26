using System.ComponentModel.DataAnnotations;

namespace ITPE3200XAPI.ViewModels;

public class EditProfileViewModel
{
    [StringLength(2000)]
    public string? Bio { get; set; }

    public string? ProfilePictureUrl { get; set; }

    public IFormFile? ImageFile { get; set; }
}