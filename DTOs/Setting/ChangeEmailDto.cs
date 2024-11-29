using System.ComponentModel.DataAnnotations;

namespace ITPE3200XAPI.DTOs.Setting;

public class ChangeEmailDto
{
    [Microsoft.Build.Framework.Required]
    [EmailAddress(ErrorMessage = "The provided email address is not valid.")]
    public string NewEmail { get; set; }
}