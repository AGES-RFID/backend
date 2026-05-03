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

    public static UserDto FromModel(User user, decimal balance = 0m) => new()
    {
        UserId = user.UserId,
        Name = user.Name,
        Email = user.Email,
        Role = user.Role,
        CreatedAt = user.CreatedAt,
        UpdatedAt = user.UpdatedAt,
        Balance = balance,
    };

}
