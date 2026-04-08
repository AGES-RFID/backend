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
    public required string PasswordHash { get; set; }
    [Required]
    [RegularExpression(@"^\d{11}$")]
    public required string Cpf { get; set; }
    [Required]
    public required string PhoneNumber { get; set; }
}
