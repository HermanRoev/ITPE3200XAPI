namespace ITPE3200XAPI.DTOs.Profile
{
    public class EditProfileDto
    {
        public string Bio { get; set; } = string.Empty;
        public IFormFile?  ProfilePicture { get; set; }
    }
}