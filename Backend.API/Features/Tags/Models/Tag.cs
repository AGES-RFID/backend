using Backend.Features.Tags.Enums;
using Backend.Features.Vehicles;

namespace Backend.Features.Tags;

public class Tag
{
    public Guid TagId { get; set; } = Guid.NewGuid();
    public TagStatus Status { get; set; } = TagStatus.AVAILABLE;
    public Vehicle? Vehicle { get; set; }
    public required string Epc { get; set; }
    public required string Tid { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
