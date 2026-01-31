using System.ComponentModel.DataAnnotations;

namespace PeopleApp.Client.Dtos;

public class RequestPasswordChangeDto
{
    [Required]
    public string CurrentPassword { get; set; } = "";
}