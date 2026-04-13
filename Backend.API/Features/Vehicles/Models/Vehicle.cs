using Backend.Features.Users;

namespace Backend.Features.Vehicles;

public class Vehicle
{
    public Guid VehicleId { get; set; } = Guid.NewGuid();
    public required Guid UserId { get; set; }
    public User? User { get; set; }

    public required string Plate { get; set; }
    public required string Brand { get; set; }
    public required string Model { get; set; }

    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
