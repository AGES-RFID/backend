namespace Backend.Features.Tags;

public class TagDto
{
    public string TagId { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public Guid? VehicleId { get; set; }

    public static TagDto FromModel(Tag tag) => new()
    {
        TagId = tag.TagId,
        Status = tag.Status.ToString(),
        VehicleId = tag.VehicleId,
    };
}
