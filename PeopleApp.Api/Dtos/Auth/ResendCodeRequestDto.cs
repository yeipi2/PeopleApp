using System.ComponentModel.DataAnnotations;

namespace PeopleApp.Api.Dtos.Auth;

public class ResendCodeRequestDto
{
    [Required]
    [EmailAddress]
    public string Email { get; set; } = "";
}