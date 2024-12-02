using System.ComponentModel.DataAnnotations;

namespace ITPE3200XAPI.DTOs.Auth
{
    public class LoginDto
    {
        [Required]
        [MinLength(3, ErrorMessage = "Email or username must be at least 3 characters long.")]
        public string? EmailOrUsername { get; set; }  // Email or username

        [Required]
        [MinLength(6, ErrorMessage = "Password must be at least 6 characters long.")]
        public string? Password { get; set; }
    }
}