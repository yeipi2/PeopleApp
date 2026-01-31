using System.ComponentModel.DataAnnotations;

namespace PeopleApp.Client.Dtos;

public class VerifyCodeRequestDto
{
    [Required]
    [EmailAddress]
    public string Email { get; set; } = "";

    [Required]
    [MinLength(6)]
    [MaxLength(6)]
    public string Code { get; set; } = "";
}