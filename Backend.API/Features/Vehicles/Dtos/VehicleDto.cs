namespace Backend.Features.Vehicles;

public class VehicleDto
{
    public Guid UserId { get; set; }
    public Guid VehicleId { get; set; } = Guid.NewGuid();
    public required string Plate { get; set; }
    public required string Brand { get; set; }
    public required string Model { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }


    public static VehicleDto FromModel(Vehicle vehicle) => new()
    {
        UserId = vehicle.UserId,
        VehicleId = vehicle.VehicleId,
        Plate = vehicle.Plate,
        Brand = vehicle.Brand,
        Model = vehicle.Model,
        CreatedAt = vehicle.CreatedAt,
        UpdatedAt = vehicle.UpdatedAt
    };
}
