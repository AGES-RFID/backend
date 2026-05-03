using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Backend.Database;
using Backend.Features.Transactions;
using Backend.Features.Users;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using tests.Setup;

namespace tests.Features.Transactions;

public class TransactionControllerTests(CustomWebApplicationFactory factory) : IClassFixture<CustomWebApplicationFactory>, IAsyncLifetime
{
    private const string JwtIssuer = "backend";
    private const string JwtAudience = "frontend";
    private const string JwtSecret = "your-super-secret-key-change-this-in-production-at-least-32-characters!";

    private readonly HttpClient _client = factory.CreateClient();
    private readonly IServiceScopeFactory _scopeFactory = factory.Services.GetRequiredService<IServiceScopeFactory>();

    public async Task InitializeAsync()
    {
        await factory.ResetDatabaseAsync();
    }

    public Task DisposeAsync() => Task.CompletedTask;

    private async Task<User> SeedUserAsync(UserRole role)
    {
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var user = new User
        {
            Name = $"User-{Guid.NewGuid()}",
            Email = $"user_{Guid.NewGuid()}@example.com",
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

    private void SetAuthHeader(User user)
    {
        var token = CreateJwtToken(user);
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
    }

    [Fact]
    public async Task CreateTransaction_AdminCanCreateForOtherUser_ReturnsCreated()
    {
        var admin = await SeedUserAsync(UserRole.Admin);
        var customer = await SeedUserAsync(UserRole.Customer);
        SetAuthHeader(admin);

        var payload = new CreateTransactionRequestDto
        {
            UserId = customer.UserId,
            Description = "Admin deposit",
            Amount = 10m
        };

        var response = await _client.PostAsync("/api/transactions", JsonContent.Create(payload, options: CustomWebApplicationFactory.JsonOptions));

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var created = await response.Content.ReadFromJsonAsync<TransactionDto>(CustomWebApplicationFactory.JsonOptions);
        Assert.NotNull(created);
        Assert.Equal(customer.UserId, created.UserId);
        Assert.Equal(payload.Amount, created.Amount);
    }

    [Fact]
    public async Task CreateTransaction_CustomerCanCreateForSelf_WhenUserIdOmitted()
    {
        var customer = await SeedUserAsync(UserRole.Customer);
        SetAuthHeader(customer);

        var payload = new CreateTransactionRequestDto
        {
            Description = "Self deposit",
            Amount = 25m
        };

        var response = await _client.PostAsync("/api/transactions", JsonContent.Create(payload, options: CustomWebApplicationFactory.JsonOptions));

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var created = await response.Content.ReadFromJsonAsync<TransactionDto>(CustomWebApplicationFactory.JsonOptions);
        Assert.NotNull(created);
        Assert.Equal(customer.UserId, created.UserId);
    }

    [Fact]
    public async Task CreateTransaction_CustomerCannotCreateForOtherUser_ReturnsForbidden()
    {
        var customer = await SeedUserAsync(UserRole.Customer);
        var otherCustomer = await SeedUserAsync(UserRole.Customer);
        SetAuthHeader(customer);

        var payload = new CreateTransactionRequestDto
        {
            UserId = otherCustomer.UserId,
            Description = "Invalid deposit",
            Amount = 10m
        };

        var response = await _client.PostAsync("/api/transactions", JsonContent.Create(payload, options: CustomWebApplicationFactory.JsonOptions));

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task CreateTransaction_AdminOmittingUserId_DefaultsToSelf()
    {
        var admin = await SeedUserAsync(UserRole.Admin);
        SetAuthHeader(admin);

        var payload = new CreateTransactionRequestDto
        {
            Description = "Admin self deposit",
            Amount = 50m
        };

        var response = await _client.PostAsync("/api/transactions", JsonContent.Create(payload, options: CustomWebApplicationFactory.JsonOptions));

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var created = await response.Content.ReadFromJsonAsync<TransactionDto>(CustomWebApplicationFactory.JsonOptions);
        Assert.NotNull(created);
        Assert.Equal(admin.UserId, created.UserId);
    }

    [Fact]
    public async Task CreateTransaction_WhenUnauthorized_ReturnsUnauthorized()
    {
        _client.DefaultRequestHeaders.Authorization = null;

        var payload = new CreateTransactionRequestDto
        {
            Description = "No auth",
            Amount = 15m
        };

        var response = await _client.PostAsync("/api/transactions", JsonContent.Create(payload, options: CustomWebApplicationFactory.JsonOptions));

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task CreateTransaction_WhenTargetUserDoesNotExist_ReturnsNotFound()
    {
        var admin = await SeedUserAsync(UserRole.Admin);
        SetAuthHeader(admin);

        var payload = new CreateTransactionRequestDto
        {
            UserId = Guid.NewGuid(),
            Description = "Missing user",
            Amount = 10m
        };

        var response = await _client.PostAsync("/api/transactions", JsonContent.Create(payload, options: CustomWebApplicationFactory.JsonOptions));

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task CreateTransaction_WhenPayloadInvalid_ReturnsBadRequest()
    {
        var admin = await SeedUserAsync(UserRole.Admin);
        SetAuthHeader(admin);

        var payload = new CreateTransactionRequestDto
        {
            Description = "",
            Amount = 0m
        };

        var response = await _client.PostAsync("/api/transactions", JsonContent.Create(payload, options: CustomWebApplicationFactory.JsonOptions));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
}
