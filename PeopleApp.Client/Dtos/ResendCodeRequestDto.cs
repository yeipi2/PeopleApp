using System.ComponentModel.DataAnnotations;

namespace PeopleApp.Client.Dtos;

public class ResendCodeRequestDto
{
    [Required]
    [EmailAddress]
    public string Email { get; set; } = "";
}