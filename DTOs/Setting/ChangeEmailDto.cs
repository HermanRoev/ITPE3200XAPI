using System.ComponentModel.DataAnnotations;

namespace ITPE3200XAPI.DTOs.Setting;

public class ChangeEmailDto
{
    [EmailAddress(ErrorMessage = "The provided email address is not valid.")]
    public string NewEmail { get; set; }
}