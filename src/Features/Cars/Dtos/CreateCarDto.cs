using System.ComponentModel.DataAnnotations;

namespace Backend.Features.Cars.Dtos;

public class CreateCarDto
{
    [Required(ErrorMessage = "Número da placa é obrigatório")]
    [StringLength(10, MinimumLength = 5, ErrorMessage = "Número da placa deve ter entre 5 e 10 caracteres")]
    public string PlateNumber { get; set; } = string.Empty;

    [Required(ErrorMessage = "ID do cliente é obrigatório")]
    public int CustomerId { get; set; }

    public int? RfidTagId { get; set; }
}
