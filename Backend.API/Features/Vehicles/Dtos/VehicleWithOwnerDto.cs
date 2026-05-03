using Backend.Features.Users;

namespace Backend.Features.Vehicles;

public class VehicleWithOwnerDto : VehicleDto
{
    public UserDto? Owner { get; init; }

    public static new VehicleWithOwnerDto FromModel(Vehicle vehicle) => new()
    {
        UserId = vehicle.UserId,
        VehicleId = vehicle.VehicleId,
        TagId = vehicle.TagId,
        Plate = vehicle.Plate,
        Brand = vehicle.Brand,
        Model = vehicle.Model,
        Owner = vehicle.User != null ? UserDto.FromModel(vehicle.User) : null,
        CreatedAt = vehicle.CreatedAt,
        UpdatedAt = vehicle.UpdatedAt
    };
}
