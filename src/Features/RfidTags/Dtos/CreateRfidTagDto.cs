using System.ComponentModel.DataAnnotations;

namespace Backend.Features.RfidTags.Dtos;

public class CreateRfidTagDto
{
    [Required(ErrorMessage = "Número da tag é obrigatório")]
    public string TagNumber { get; set; } = string.Empty;
}
