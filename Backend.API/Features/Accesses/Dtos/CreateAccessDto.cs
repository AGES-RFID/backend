using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Backend.Features.Accesses;

public class CreateAccessDto
{
    [Required(ErrorMessage = "O tag_id é obrigatório.")]
    [JsonPropertyName("tag_id")]
    public required string TagId { get; set; }
}
