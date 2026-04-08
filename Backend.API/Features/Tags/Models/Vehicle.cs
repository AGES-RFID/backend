using Backend.Features.Users;

namespace Backend.Features.Tags;
public class Vehicle
{
    public Guid VehicleId { get; set; } = Guid.NewGuid();
    public required string LicensePlate { get; set; }
    public required string Model { get; set; }
    public required string Color { get; set; }

    public Tag? Tag { get; set; }

    public string? TagId { get; set; }

    public User User { get; set; } = null!;
    public Guid UserId { get; set; }
}