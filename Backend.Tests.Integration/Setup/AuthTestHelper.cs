using System.IdentityModel.Tokens.Jwt;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text;
using Backend.Database;
using Backend.Features.Users;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;

namespace tests.Setup;

public static class AuthTestHelper
{
    private const string JwtIssuer = "backend";
    private const string JwtAudience = "frontend";
    private const string JwtSecret = "your-super-secret-key-change-this-in-production-at-least-32-characters!";

    public static HttpClient CreateAnonymousClient(CustomWebApplicationFactory factory)
        => factory.CreateClient();

    public static string CreateTokenForUser(User user) => CreateJwtToken(user);

    public static async Task<HttpClient> CreateClientAsAsync(
        CustomWebApplicationFactory factory,
        UserRole role,
        string? name = null,
        string? email = null)
    {
        var scopeFactory = factory.Services.GetRequiredService<IServiceScopeFactory>();
        var user = await SeedUserAsync(scopeFactory, role, name, email);

        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", CreateJwtToken(user));

        return client;
    }

    private static async Task<User> SeedUserAsync(
        IServiceScopeFactory scopeFactory,
        UserRole role,
        string? name,
        string? email)
    {
        using var scope = scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var user = new User
        {
            Name = name ?? $"User-{Guid.NewGuid()}",
            Email = email ?? $"user_{Guid.NewGuid()}@example.com",
            PasswordHash = "hash",
            Role = role
        };

        db.Users.Add(user);
        await db.SaveChangesAsync();

        return user;
    }

    private static string CreateJwtToken(User user)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(JwtSecret));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.UserId.ToString()),
            new("role", user.Role.ToString())
        };

        var token = new JwtSecurityToken(
            issuer: JwtIssuer,
            audience: JwtAudience,
            claims: claims,
            expires: DateTime.UtcNow.AddHours(1),
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
