using System.ComponentModel.DataAnnotations;

namespace Backend.Features.AccountBalance.Dtos;

public class WithdrawDto
{
    [Required(ErrorMessage = "Valor é obrigatório")]
    [Range(0.01, double.MaxValue, ErrorMessage = "Valor deve ser maior que 0")]
    public decimal Amount { get; set; }

    [Required(ErrorMessage = "ID do veículo é obrigatório")]
    public int VehicleId { get; set; }

    [Required(ErrorMessage = "Placa do veículo é obrigatória")]
    [StringLength(10, ErrorMessage = "Placa do veículo não pode exceder 10 caracteres")]
    public string VehiclePlate { get; set; } = string.Empty;

    [StringLength(500, ErrorMessage = "Descrição não pode exceder 500 caracteres")]
    public string? Description { get; set; }
}
