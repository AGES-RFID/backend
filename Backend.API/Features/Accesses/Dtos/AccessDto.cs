namespace Backend.Features.Accesses;

using System.Text.Json.Serialization;

public class AccessDto
{
    [JsonPropertyName("access_id")]
    public Guid AccessId { get; set; }

    [JsonPropertyName("tag_id")]
    public required string TagId { get; set; }

    public AccessType Type { get; set; }
    public DateTime Timestamp { get; set; }

    public static AccessDto FromModel(Access access) => new()
    {
        AccessId = access.AccessId,
        TagId = access.TagId,
        Type = access.Type,
        Timestamp = access.Timestamp
    };
}
