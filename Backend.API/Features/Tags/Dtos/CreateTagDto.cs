namespace Backend.Features.Tags;

using System.ComponentModel.DataAnnotations;

public class CreateTagDto
{
    [Required(ErrorMessage = "TagId is required")]
    public required string TagId { get; set; }
}
