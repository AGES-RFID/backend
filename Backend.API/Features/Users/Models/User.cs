namespace Backend.Features.Users;

using Backend.Features.Vehicles;
public class User
{
    public Guid UserId { get; set; } = Guid.NewGuid();
    public required string Name { get; set; }
    public required string Email { get; set; }
    public required string PasswordHash { get; set; }
    public required UserRole Role { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public ICollection<Vehicle> Vehicles { get; set; } = [];
}
