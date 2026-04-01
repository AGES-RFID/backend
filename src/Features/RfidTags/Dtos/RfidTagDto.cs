namespace Backend.Features.RfidTags.Dtos;

public class RfidTagDto
{
    public int Id { get; set; }
    public string TagNumber { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? DeactivatedAt { get; set; }
    public DateTime? ReactivatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
