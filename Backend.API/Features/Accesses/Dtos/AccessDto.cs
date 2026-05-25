namespace Backend.Features.Accesses;

public class AccessDto
{
    public Guid AccessId { get; set; }
    public required Guid TagId { get; set; }
    public AccessType Type { get; set; }
    public DateTime Timestamp { get; set; }
    public string? Plate { get; set; }
    public decimal? Value { get; set; }

    public static AccessDto FromModel(Access access) => new()
    {
        AccessId = access.AccessId,
        TagId = access.TagId,
        Type = access.Type,
        Timestamp = access.Timestamp,
        Plate = null,
        Value = null
    };
}
