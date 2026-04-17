using Backend.Features.Users;

namespace Backend.Features.Vehicles;

public class VehicleDto
{
    public required Guid UserId { get; init; }
    public required Guid VehicleId { get; init; }
    public string? TagId { get; init; }
    public required string Plate { get; init; }
    public required string Brand { get; init; }
    public required string Model { get; init; }
    public UserDto? Owner { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime UpdatedAt { get; init; }

    public static VehicleDto FromModel(Vehicle vehicle) => new()
    {
        UserId = vehicle.UserId,
        VehicleId = vehicle.VehicleId,
        TagId = vehicle.TagId,
        Plate = vehicle.Plate,
        Brand = vehicle.Brand,
        Model = vehicle.Model,
        Owner = vehicle.User is null ? null : UserDto.FromModel(vehicle.User),
        CreatedAt = vehicle.CreatedAt,
        UpdatedAt = vehicle.UpdatedAt
    };
}
