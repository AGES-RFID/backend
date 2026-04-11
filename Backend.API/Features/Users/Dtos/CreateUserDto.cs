namespace Backend.Features.Users;

using System.ComponentModel.DataAnnotations;

public class CreateUserDto
{
    [Required]
    [MinLength(1)]
    public required string Name { get; set; }
    [Required]
    [EmailAddress]
    public required string Email { get; set; }
    [Required]
    [MinLength(8)]
    public required string Password { get; set; }
    [Required]
    public required UserRole Role { get; set; }
}
