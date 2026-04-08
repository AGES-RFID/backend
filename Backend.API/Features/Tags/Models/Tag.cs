using Backend.Features.Accesses;
using Backend.Features.Vehicles;

namespace Backend.Features.Tags;

public class Tag
{
    public Guid TagId { get; set; } = Guid.NewGuid();
    public Guid? VeichleId { get; set; }
    public TagStatus Status { get; set; } = TagStatus.Available;

    public Vehicle? Veichle { get; set; }
    public ICollection<Access> Accesses { get; set; } = [];
}
