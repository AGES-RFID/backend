using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Backend.Configuration;
using Backend.Database;
using Backend.Features.Users;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
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

public class AuthService(AppDbContext db, IOptions<JwtSettings> jwtSettings) : IAuthService
{
    private readonly AppDbContext _db = db;
    private readonly JwtSettings _jwtSettings = jwtSettings.Value;

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
        var secretKey = _jwtSettings.SecretKey;
        var issuer = _jwtSettings.Issuer;
        var audience = _jwtSettings.Audience;
        var expiryMinutes = _jwtSettings.ExpiryMinutes;

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
