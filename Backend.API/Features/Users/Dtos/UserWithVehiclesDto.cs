namespace Backend.Features.Users;

using Backend.Features.Vehicles;

public class UserWithVehiclesDto : UserDto
{
    public ICollection<VehicleDto>? Vehicles { get; set; }

    public static new UserWithVehiclesDto FromModel(User user, decimal balance = 0m) => new()
    {
        UserId = user.UserId,
        Name = user.Name,
        Email = user.Email,
        Role = user.Role,
        CreatedAt = user.CreatedAt,
        UpdatedAt = user.UpdatedAt,
        Balance = balance,
        Vehicles = [.. user.Vehicles.Select(VehicleDto.FromModel)]
    };
}
