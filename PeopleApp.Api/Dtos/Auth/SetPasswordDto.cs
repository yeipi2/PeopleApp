using System.ComponentModel.DataAnnotations;

namespace PeopleApp.Api.Dtos.Auth;

public class SetPasswordDto
{
    [Required]
    [MinLength(8)]
    public string NewPassword { get; set; } = "";

    [Required]
    [Compare(nameof(NewPassword))]
    public string ConfirmPassword { get; set; } = "";
}