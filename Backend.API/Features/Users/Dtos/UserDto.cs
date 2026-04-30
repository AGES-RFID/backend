namespace Backend.Features.Users;

using Backend.Features.Vehicles;

public class UserDto
{
    public Guid UserId { get; set; }
    public required string Name { get; set; }
    public required string Email { get; set; }
    public required UserRole Role { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public decimal Balance { get; set; }
    public ICollection<Vehicle> Vehicles { get; set; } = [];


    public static UserDto FromModel(User user) => new()
    {
        UserId = user.UserId,
        Name = user.Name,
        Email = user.Email,
        Role = user.Role,
        CreatedAt = user.CreatedAt,
        UpdatedAt = user.UpdatedAt,
        Balance = user.Balance,
        Vehicles = user.Vehicles.Select(v => new Vehicle
        {
            UserId = v.UserId,
            VehicleId = v.VehicleId,
            Plate = v.Plate,
            Brand = v.Brand,
            Model = v.Model,
            TagId = v.TagId,
            CreatedAt = v.CreatedAt,
            UpdatedAt = v.UpdatedAt
        }).ToList()
    };

}
