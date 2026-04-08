using Backend.Features.Tags;
using Backend.Features.Users;

namespace Backend.Features.Vehicles;

public class Vehicle
{
    public Guid VehicleId { get; set; } = Guid.NewGuid();
    public required Guid UserId { get; set; }
    public required string Plate { get; set; }
    public required string Brand { get; set; }
    public required string Model { get; set; }
    public required string Color { get; set; }

    public User User { get; set; } = null!;
    public ICollection<Tag> Tags { get; set; } = [];
}
