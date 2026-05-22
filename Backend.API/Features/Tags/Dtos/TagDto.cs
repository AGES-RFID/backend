namespace Backend.Features.Tags;

public class TagDto
{
    public required Guid TagId { get; set; }
    public string Status { get; set; } = string.Empty;
    public Guid? VehicleId { get; set; }
    public required string Epc { get; set; }
    public required string Tid { get; set; }

    public static TagDto FromModel(Tag tag) => new()
    {
        TagId = tag.TagId,
        Status = tag.Status.ToString(),
        VehicleId = tag.Vehicle?.VehicleId,
        Epc = tag.Epc,
        Tid = tag.Tid
    };
}
