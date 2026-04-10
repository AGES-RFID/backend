namespace Backend.Features.Vehicles;

public class Vehicle
{
    public Guid UserId { get; set; }
    public Guid VehicleId { get; set; } = Guid.NewGuid();
    public required string Model { get; set; }
    public required string Brand { get; set; }
    public required string Plate { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
