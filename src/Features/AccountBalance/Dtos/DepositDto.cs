using System.ComponentModel.DataAnnotations;

namespace Backend.Features.AccountBalance.Dtos;

public class DepositDto
{
    [Required(ErrorMessage = "Valor é obrigatório")]
    [Range(0.01, double.MaxValue, ErrorMessage = "Valor deve ser maior que 0")]
    public decimal Amount { get; set; }
}
