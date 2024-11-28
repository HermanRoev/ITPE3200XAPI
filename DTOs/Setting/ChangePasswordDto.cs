using System.ComponentModel.DataAnnotations;

namespace ITPE3200XAPI.DTOs.Setting;

public class ChangePasswordDto
{
    [Required]
    [MinLength(6, ErrorMessage = "Password must be at least 6 characters long.")]
    public String OldPassword { get; set; }

    [Required]
    [MinLength(6, ErrorMessage = "Password must be at least 6 characters long.")]
    public String NewPassword { get; set; }
    
    [Required]
    [Compare("Password", ErrorMessage = "Passwords do not match.")]
    public String ConfirmNewPassword { get; set; } 
}