using Backend.Features.Tags;

namespace Backend.Features.Accesses;

public class Access
{
    public Guid AccessId { get; set; } = Guid.NewGuid();
    public required Guid TagId { get; set; }
    public required AccessType Type { get; set; }

    public required Tag Tag { get; set; }

    public required DateTime Timestamp { get; set; }

    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
