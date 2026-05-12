using Backend.Features.Tags.Enums;
using Backend.Features.Vehicles;

namespace Backend.Features.Tags;

public class Tag
{
    public string TagId { get; set; } = string.Empty;
    public TagStatus Status { get; set; } = TagStatus.AVAILABLE;
    public Vehicle? Vehicle { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public string Epc { get; set; } = string.Empty;
}
