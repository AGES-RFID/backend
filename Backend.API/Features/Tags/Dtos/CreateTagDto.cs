namespace Backend.Features.Tags;

using System.ComponentModel.DataAnnotations;

public class CreateTagDto
{
    [Required]
    public required string Epc { get; set; }

    [Required]
    public required string Tid { get; set; }
}
