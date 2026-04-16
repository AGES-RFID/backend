using Backend.Configuration;
using Backend.Database;
using Backend.Features.Auth;
using Backend.Features.Users;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Backend.Tests.Integration.Features.Auth;

public class AuthServiceTests
{
    private readonly AppDbContext _db;
    private readonly AuthService _sut;

    public AuthServiceTests()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        _db = new AppDbContext(options);

        _sut = new AuthService(_db, Options.Create(new JwtSettings
        {
            SecretKey = "supersecretkeyshouldbeverylong1234567890",
            Issuer = "TestIssuer",
            Audience = "TestAudience",
        }));
    }

    private User SeedUser(string email = "alice@example.com", string password = "password123")
    {
        var user = new User
        {
            UserId = Guid.NewGuid(),
            Name = "Alice",
            Email = email,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(password),
            Role = UserRole.Customer,
        };
        _db.Users.Add(user);
        _db.SaveChanges();
        return user;
    }

    [Fact]
    public async Task LoginAsync_WhenCredentialsAreValid_ReturnsAuthResponse()
    {
        var user = SeedUser();

        var result = await _sut.LoginAsync(new LoginDto
        {
            Email = user.Email,
            Password = "password123"
        });

        Assert.NotNull(result);
        Assert.NotNull(result.Token);
        Assert.NotNull(result.User);
        Assert.Equal(user.UserId, result.User.UserId);
        Assert.Equal(user.Email, result.User.Email);
    }

    [Fact]
    public async Task LoginAsync_WhenPasswordIsWrong_ThrowsInvalidCredentialsException()
    {
        SeedUser(password: "correctpassword");

        await Assert.ThrowsAsync<InvalidCredentialsException>(() =>
            _sut.LoginAsync(new LoginDto
            {
                Email = "alice@example.com",
                Password = "wrongpassword"
            }));
    }

    [Fact]
    public async Task LoginAsync_WhenUserDoesNotExist_ThrowsInvalidCredentialsException()
    {
        await Assert.ThrowsAsync<InvalidCredentialsException>(() =>
            _sut.LoginAsync(new LoginDto
            {
                Email = "nobody@example.com",
                Password = "password123"
            }));
    }
}
