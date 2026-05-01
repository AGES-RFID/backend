using Backend.Features.Tags.Enums;
using Backend.Features.Vehicles;

namespace Backend.Features.Tags;

public class Tag
{
    public required string TagId { get; set; }
    public string? Epc { get; set; }
    public TagStatus Status { get; set; } = TagStatus.AVAILABLE;
    public Vehicle? Vehicle { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
