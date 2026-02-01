using System.ComponentModel.DataAnnotations;

namespace PeopleApp.Client.Dtos;

public class SetPasswordDto
{
    [Required(ErrorMessage = "Nueva contraseña es requerida")]
    [MinLength(8, ErrorMessage = "Mínimo 8 caracteres")]
    public string NewPassword { get; set; } = "";

    [Required(ErrorMessage = "Confirmación es requerida")]
    [Compare(nameof(NewPassword), ErrorMessage = "Las contraseñas no coinciden")]
    public string ConfirmPassword { get; set; } = "";
}