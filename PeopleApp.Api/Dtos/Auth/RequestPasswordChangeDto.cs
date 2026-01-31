using System.ComponentModel.DataAnnotations;

namespace PeopleApp.Api.Dtos.Auth;

public class RequestPasswordChangeDto
{
    [Required]
    public string CurrentPassword { get; set; } = "";
}