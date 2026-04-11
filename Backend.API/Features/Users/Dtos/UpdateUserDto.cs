namespace Backend.Features.Users;

using System.ComponentModel.DataAnnotations;

public class UpdateUserDto
{
    public string? Name { get; set; }
    [EmailAddress]
    public string? Email { get; set; }
    [MinLength(8)]
    public string? Password { get; set; }
    public UserRole? Role { get; set; }
}
