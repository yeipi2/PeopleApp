using System.ComponentModel.DataAnnotations;

namespace PeopleApp.Client.Dtos;

public class Toggle2FARequestDto
{
    [Required]
    public bool Enabled { get; set; }
}