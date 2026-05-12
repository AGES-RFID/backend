using System.ComponentModel.DataAnnotations;

namespace Backend.Features.Accesses;

public class CreateAccessDto
{
    [Required(ErrorMessage = "TagId is required.")]
    public required string TagId { get; set; }
}
