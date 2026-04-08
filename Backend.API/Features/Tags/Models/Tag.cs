namespace Backend.Features.Tags;
using Backend.Features.Tags.Enums;

public class Tag
{
    public required string TagId { get; set; }
    public TagStatus Status { get; set; } = TagStatus.AVAILABLE;
    public Guid? VehicleId { get; set; }
    public Vehicle? Vehicle { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
