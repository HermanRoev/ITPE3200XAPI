using System.ComponentModel.DataAnnotations;

namespace ITPE3200XAPI.DTOs.Auth
{
    public class LoginDto
    {
        [Required]
        [EmailAddress(ErrorMessage = "Invalid email address.")]
        public string Email { get; set; }

        [Required]
        [MinLength(6, ErrorMessage = "Password must be at least 6 characters long.")]
        public string Password { get; set; }
    }
}