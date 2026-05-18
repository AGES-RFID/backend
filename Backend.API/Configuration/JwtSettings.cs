using System.ComponentModel.DataAnnotations;

namespace Backend.Configuration;

public class JwtSettings
{
    [Required]
    public string SecretKey { get; set; } = null!;
    public string? Issuer { get; init; }
    public string? Audience { get; init; }
    [Range(1, int.MaxValue, ErrorMessage = "Expiry minutes must be a positive integer")]
    public int ExpiryMinutes { get; init; } = 180;
}
