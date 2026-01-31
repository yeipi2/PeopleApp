using System.ComponentModel.DataAnnotations;

namespace PeopleApp.Api.Dtos.Auth;

public class VerifyPasswordChangeDto
{
    [Required]
    [MinLength(6)]
    [MaxLength(6)]
    public string Code { get; set; } = "";

    [Required]
    [MinLength(8)]
    public string NewPassword { get; set; } = "";

    [Required]
    [Compare(nameof(NewPassword))]
    public string ConfirmPassword { get; set; } = "";
}