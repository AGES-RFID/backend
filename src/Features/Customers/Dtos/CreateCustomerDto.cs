using System.ComponentModel.DataAnnotations;

namespace Backend.Features.Customers.Dtos;

public class CreateCustomerDto
{
    [Required(ErrorMessage = "Email é obrigatório")]
    [EmailAddress(ErrorMessage = "Formato de email inválido")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Nome é obrigatório")]
    [MinLength(3, ErrorMessage = "Nome deve ter pelo menos 3 caracteres")]
    public string Name { get; set; } = string.Empty;

    [Required(ErrorMessage = "CPF é obrigatório")]
    [StringLength(11, MinimumLength = 11, ErrorMessage = "CPF deve ter exatamente 11 caracteres")]
    [RegularExpression(@"^\d{11}$", ErrorMessage = "CPF deve conter apenas números")]
    public string Cpf { get; set; } = string.Empty;

    [Required(ErrorMessage = "Celular é obrigatório")]
    [Phone(ErrorMessage = "Formato de celular inválido")]
    public string Cellphone { get; set; } = string.Empty;

    [Required(ErrorMessage = "CEP é obrigatório")]
    [StringLength(8, MinimumLength = 8, ErrorMessage = "CEP deve ter exatamente 8 caracteres")]
    [RegularExpression(@"^\d{8}$", ErrorMessage = "CEP deve conter apenas números")]
    public string Cep { get; set; } = string.Empty;

    [Required(ErrorMessage = "Endereço é obrigatório")]
    [MinLength(5, ErrorMessage = "Endereço deve ter pelo menos 5 caracteres")]
    public string Address { get; set; } = string.Empty;
}
