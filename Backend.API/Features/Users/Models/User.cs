using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Identity;

namespace Backend.Features.Users;

public class User
{
    public Guid UserId { get; set; } = Guid.NewGuid();
    public required string Name { get; set; }
    public required string Email { get; set; }
    public required string PasswordHash { get; set; }
    public required string Cpf { get; set; }
    public required string PhoneNumber { get; set; }
    public UserRole Role { get; set; } = UserRole.Client;
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
