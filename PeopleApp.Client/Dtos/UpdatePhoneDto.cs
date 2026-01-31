using System.ComponentModel.DataAnnotations;

namespace PeopleApp.Client.Dtos;

public class UpdatePhoneDto
{
    [Required]
    [MinLength(10)]
    [MaxLength(15)]
    public string PhoneNumber { get; set; } = "";
}