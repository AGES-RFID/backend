using Backend.Features.Tags;

namespace Backend.Features.Accesses;

public class Accesses
{
    public Guid AccessesId { get; set; } = Guid.NewGuid();
    public required string TagId { get; set; }
    public required AcessType Type { get; set; }

    public required Tag Tag { get; set; }

    public required DateTime Timestamp { get; set; }

    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}