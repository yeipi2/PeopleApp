using System.ComponentModel.DataAnnotations;

namespace PeopleApp.Client.Dtos;

public class GoogleLoginRequestDto
{
    [Required]
    public string IdToken { get; set; } = string.Empty;
}