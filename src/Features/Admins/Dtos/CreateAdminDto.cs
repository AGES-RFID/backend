using System.ComponentModel.DataAnnotations;

namespace Backend.Features.Admins.Dtos;

public class CreateAdminDto
{
    [Required(ErrorMessage = "Email é obrigatório")]
    [EmailAddress(ErrorMessage = "Formato de email inválido")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Nome é obrigatório")]
    [MinLength(3, ErrorMessage = "Nome deve ter pelo menos 3 caracteres")]
    public string Name { get; set; } = string.Empty;
}
