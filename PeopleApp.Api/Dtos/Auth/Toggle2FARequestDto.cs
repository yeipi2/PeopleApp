using System.ComponentModel.DataAnnotations;

namespace PeopleApp.Api.Dtos.Auth;

public class Toggle2FARequestDto
{
    [Required]
    public bool Enabled { get; set; }
}