namespace Backend.Features.RfidTags.Models;

public class RfidTag
{
    public int RfidTagId { get; set; }
    public required string TagNumber { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? DeactivatedAt { get; set; }
    public DateTime? ReactivatedAt { get; set; }
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
