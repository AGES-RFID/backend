using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Backend.Database;
using Backend.Features.Users;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

namespace Backend.Features.Auth;

public class InvalidCredentialsException : Exception
{
    public InvalidCredentialsException() : base("Email ou senha inválidos") { }
}

public interface IAuthService
{
    Task<AuthResponse> LoginAsync(LoginDto dto);
}

public class AuthService(AppDbContext db, IConfiguration config) : IAuthService
{
    private readonly AppDbContext _db = db;
    private readonly IConfiguration _config = config;

    public async Task<AuthResponse> LoginAsync(LoginDto dto)
    {
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Email == dto.Email)
            ?? throw new InvalidCredentialsException();

        var isPasswordValid = BCrypt.Net.BCrypt.Verify(dto.Password, user.PasswordHash);
        if (!isPasswordValid)
        {
            throw new InvalidCredentialsException();
        }

        var token = GenerateJwtToken(user);
        var userDto = UserDto.FromModel(user);

        return new AuthResponse
        {
            Token = token,
            User = userDto
        };
    }

    private string GenerateJwtToken(User user)
    {
        var jwtSettings = _config.GetSection("Jwt");
        var secretKey = jwtSettings["SecretKey"]
            ?? throw new InvalidOperationException("JWT SecretKey not configured");
        var issuer = jwtSettings["Issuer"];
        var audience = jwtSettings["Audience"];
        var expiryMinutes = int.Parse(jwtSettings["ExpiryMinutes"] ?? "180");

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, user.UserId.ToString()),
            new Claim(JwtRegisteredClaimNames.Email, user.Email),
            new Claim(JwtRegisteredClaimNames.Name, user.Name),
            new Claim("role", user.Role.ToString())
        };

        var token = new JwtSecurityToken(
            issuer: issuer,
            audience: audience,
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(expiryMinutes),
            signingCredentials: credentials
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
