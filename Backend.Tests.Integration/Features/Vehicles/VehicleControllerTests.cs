using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text;
using Backend.Database;
using Backend.Features.Users;
using Backend.Features.Vehicles;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using tests.Setup;

namespace tests.Features.Vehicles;

public class VehicleControllerTests(CustomWebApplicationFactory factory) : IClassFixture<CustomWebApplicationFactory>, IAsyncLifetime
{
    private const string JwtIssuer = "backend";
    private const string JwtAudience = "frontend";
    private const string JwtSecret = "your-super-secret-key-change-this-in-production-at-least-32-characters!";

    private readonly HttpClient _client = factory.CreateClient();
    private readonly IServiceScopeFactory _scopeFactory = factory.Services.GetRequiredService<IServiceScopeFactory>();

    public async Task InitializeAsync()
    {
        await factory.ResetDatabaseAsync();
        _client.DefaultRequestHeaders.Authorization = null;
    }

    public Task DisposeAsync() => Task.CompletedTask;

    private async Task<User> SeedUserAsync(UserRole role = UserRole.Admin)
    {
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var user = new User
        {
            Name = $"User-{Guid.NewGuid()}",
            Email = $"test_{Guid.NewGuid()}@email.com",
            PasswordHash = "dummy_hash",
            Role = role
        };

        db.Users.Add(user);
        await db.SaveChangesAsync();
        return user;
    }

    private async Task<Vehicle> SeedVehicleAsync(User owner, string plate)
    {
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var vehicle = new Vehicle
        {
            UserId = owner.UserId,
            Plate = plate,
            Brand = "Honda",
            Model = "HRV"
        };

        db.Vehicles.Add(vehicle);
        await db.SaveChangesAsync();
        return vehicle;
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
    public async Task GetAllVehicles_WhenUnauthenticated_ReturnsUnauthorized()
    {
        _client.DefaultRequestHeaders.Authorization = null;

        var response = await _client.GetAsync("/api/vehicles");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task CreateVehicle_WithValidPayload_ReturnsCreated()
    {
        var admin = await SeedUserAsync(UserRole.Admin);
        SetAuthHeader(admin);

        var payload = new CreateVehicleDto { Brand = "Honda", Model = "HRV", Plate = "AAA9A99", UserId = admin.UserId };

        var response = await _client.PostAsync("/api/vehicles", JsonContent.Create(payload));

        response.EnsureSuccessStatusCode();
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var created = await response.Content.ReadFromJsonAsync<VehicleDto>();
        Assert.NotNull(created);
        Assert.Equal(payload.Plate, created.Plate);
        Assert.Equal(payload.UserId!.Value, created.UserId);
        Assert.NotEqual(Guid.Empty, created.VehicleId);
    }

    [Fact]
    public async Task CreateVehicle_WhenCustomerOmitsUserId_UsesAuthenticatedUser()
    {
        var customer = await SeedUserAsync(UserRole.Customer);
        SetAuthHeader(customer);

        var payload = new CreateVehicleDto { Brand = "Honda", Model = "HRV", Plate = "CUS0A99" };

        var response = await _client.PostAsync("/api/vehicles", JsonContent.Create(payload));

        response.EnsureSuccessStatusCode();
        var created = await response.Content.ReadFromJsonAsync<VehicleDto>();
        Assert.NotNull(created);
        Assert.Equal(customer.UserId, created.UserId);
    }

    [Fact]
    public async Task CreateVehicle_WhenCustomerCreatesForOtherUser_ReturnsNotFound()
    {
        var customer = await SeedUserAsync(UserRole.Customer);
        var otherUser = await SeedUserAsync(UserRole.Customer);
        SetAuthHeader(customer);

        var payload = new CreateVehicleDto { Brand = "Honda", Model = "HRV", Plate = "CUS9A99", UserId = otherUser.UserId };

        var response = await _client.PostAsync("/api/vehicles", JsonContent.Create(payload));

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task CreateVehicle_WithExistingPlate_ReturnsConflict()
    {
        var admin = await SeedUserAsync(UserRole.Admin);
        SetAuthHeader(admin);

        var payload = new CreateVehicleDto { Brand = "Honda", Model = "HRV", Plate = "SAMEPLT", UserId = admin.UserId };

        await _client.PostAsync("/api/vehicles", JsonContent.Create(payload));

        var response = await _client.PostAsync("/api/vehicles", JsonContent.Create(payload));

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task SearchVehicleByPlate_WithExistingPlate_ReturnsOkWithDetails()
    {
        var admin = await SeedUserAsync(UserRole.Admin);
        SetAuthHeader(admin);

        var payload = new CreateVehicleDto { Brand = "Honda", Model = "HRV", Plate = "SEARCH1", UserId = admin.UserId };
        await _client.PostAsync("/api/vehicles", JsonContent.Create(payload));

        var response = await _client.GetAsync("/api/vehicles/search?plate=SEARCH1");

        response.EnsureSuccessStatusCode();
        var fetched = await response.Content.ReadFromJsonAsync<VehicleSearchResponseDto>();

        Assert.NotNull(fetched);
        Assert.Equal("SEARCH1", fetched.Plate);
        Assert.Equal(admin.Name, fetched.OwnerName);
        Assert.NotEqual(Guid.Empty, fetched.VehicleId);
    }

    [Fact]
    public async Task SearchVehicleByPlate_WhenCustomerSearchesOtherUserVehicle_ReturnsNotFound()
    {
        var owner = await SeedUserAsync(UserRole.Customer);
        await SeedVehicleAsync(owner, "HIDE999");

        var customer = await SeedUserAsync(UserRole.Customer);
        SetAuthHeader(customer);

        var response = await _client.GetAsync("/api/vehicles/search?plate=HIDE999");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task GetVehicleById_WhenCustomerRequestsOtherUsersVehicle_ReturnsNotFound()
    {
        var owner = await SeedUserAsync(UserRole.Customer);
        var vehicle = await SeedVehicleAsync(owner, "OWNR001");

        var customer = await SeedUserAsync(UserRole.Customer);
        SetAuthHeader(customer);

        var response = await _client.GetAsync($"/api/vehicles/{vehicle.VehicleId}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task GetAllVehicles_WhenCustomer_ReturnsOnlyOwnVehicles()
    {
        var owner = await SeedUserAsync(UserRole.Customer);
        await SeedVehicleAsync(owner, "OWN1111");

        var other = await SeedUserAsync(UserRole.Customer);
        await SeedVehicleAsync(other, "OTH2222");

        SetAuthHeader(owner);

        var response = await _client.GetAsync("/api/vehicles");

        response.EnsureSuccessStatusCode();
        var fetched = await response.Content.ReadFromJsonAsync<List<VehicleDto>>();

        Assert.NotNull(fetched);
        Assert.Single(fetched);
        Assert.Equal("OWN1111", fetched[0].Plate);
    }

    [Fact]
    public async Task UpdateVehicle_WhenCustomerUpdatesOtherUsersVehicle_ReturnsNotFound()
    {
        var owner = await SeedUserAsync(UserRole.Customer);
        var vehicle = await SeedVehicleAsync(owner, "UPD0001");

        var customer = await SeedUserAsync(UserRole.Customer);
        SetAuthHeader(customer);

        var updatePayload = new CreateVehicleDto { Brand = "Toyota", Model = "Corolla", Plate = "UPD0002", UserId = customer.UserId };

        var response = await _client.PutAsync($"/api/vehicles/{vehicle.VehicleId}", JsonContent.Create(updatePayload));

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task CreateVehicle_WhenAdminOmitsUserId_DefaultsToAuthenticatedAdmin()
    {
        var admin = await SeedUserAsync(UserRole.Admin);
        SetAuthHeader(admin);

        var payload = new CreateVehicleDto { Brand = "Honda", Model = "HRV", Plate = "ADM0A99" };

        var response = await _client.PostAsync("/api/vehicles", JsonContent.Create(payload));

        response.EnsureSuccessStatusCode();
        var created = await response.Content.ReadFromJsonAsync<VehicleDto>();
        Assert.NotNull(created);
        Assert.Equal(admin.UserId, created.UserId);
    }

    [Fact]
    public async Task CreateVehicle_WhenCustomerProvidesOwnUserId_ReturnsCreated()
    {
        var customer = await SeedUserAsync(UserRole.Customer);
        SetAuthHeader(customer);

        var payload = new CreateVehicleDto { Brand = "Honda", Model = "HRV", Plate = "CUS1A99", UserId = customer.UserId };

        var response = await _client.PostAsync("/api/vehicles", JsonContent.Create(payload));

        response.EnsureSuccessStatusCode();
        var created = await response.Content.ReadFromJsonAsync<VehicleDto>();
        Assert.NotNull(created);
        Assert.Equal(customer.UserId, created.UserId);
    }

    [Fact]
    public async Task UpdateVehicle_WhenAdminOmitsUserId_KeepsCurrentOwner()
    {
        var owner = await SeedUserAsync(UserRole.Customer);
        var vehicle = await SeedVehicleAsync(owner, "ADMUPD1");

        var admin = await SeedUserAsync(UserRole.Admin);
        SetAuthHeader(admin);

        var payload = new CreateVehicleDto { Brand = "Toyota", Model = "Corolla", Plate = "ADMUPD2" };

        var response = await _client.PutAsync($"/api/vehicles/{vehicle.VehicleId}", JsonContent.Create(payload));

        response.EnsureSuccessStatusCode();
        var updated = await response.Content.ReadFromJsonAsync<VehicleDto>();
        Assert.NotNull(updated);
        Assert.Equal(owner.UserId, updated.UserId);
        Assert.Equal("ADMUPD2", updated.Plate);
    }

    [Fact]
    public async Task UpdateVehicle_WhenAdminSetsNewOwner_TransfersOwnership()
    {
        var owner = await SeedUserAsync(UserRole.Customer);
        var vehicle = await SeedVehicleAsync(owner, "TRN0001");
        var newOwner = await SeedUserAsync(UserRole.Customer);

        var admin = await SeedUserAsync(UserRole.Admin);
        SetAuthHeader(admin);

        var payload = new CreateVehicleDto { Brand = "Honda", Model = "HRV", Plate = "TRN0002", UserId = newOwner.UserId };

        var response = await _client.PutAsync($"/api/vehicles/{vehicle.VehicleId}", JsonContent.Create(payload));

        response.EnsureSuccessStatusCode();
        var updated = await response.Content.ReadFromJsonAsync<VehicleDto>();
        Assert.NotNull(updated);
        Assert.Equal(newOwner.UserId, updated.UserId);
    }

    [Fact]
    public async Task UpdateVehicle_WhenCustomerUpdatesOwnVehicleWithoutUserId_UsesAuthenticatedOwner()
    {
        var customer = await SeedUserAsync(UserRole.Customer);
        var vehicle = await SeedVehicleAsync(customer, "CUP0001");
        SetAuthHeader(customer);

        var payload = new CreateVehicleDto { Brand = "Toyota", Model = "Yaris", Plate = "CUP0002" };

        var response = await _client.PutAsync($"/api/vehicles/{vehicle.VehicleId}", JsonContent.Create(payload));

        response.EnsureSuccessStatusCode();
        var updated = await response.Content.ReadFromJsonAsync<VehicleDto>();
        Assert.NotNull(updated);
        Assert.Equal(customer.UserId, updated.UserId);
    }

    [Fact]
    public async Task UpdateVehicle_WhenCustomerTriesToTransferOwnVehicle_ReturnsNotFound()
    {
        var customer = await SeedUserAsync(UserRole.Customer);
        var vehicle = await SeedVehicleAsync(customer, "CNS0001");
        var otherUser = await SeedUserAsync(UserRole.Customer);
        SetAuthHeader(customer);

        var payload = new CreateVehicleDto { Brand = "Honda", Model = "Fit", Plate = "CNS0002", UserId = otherUser.UserId };

        var response = await _client.PutAsync($"/api/vehicles/{vehicle.VehicleId}", JsonContent.Create(payload));

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task DeleteVehicle_WhenCustomerDeletesOtherUsersVehicle_ReturnsNotFound()
    {
        var owner = await SeedUserAsync(UserRole.Customer);
        var vehicle = await SeedVehicleAsync(owner, "DEL0001");

        var customer = await SeedUserAsync(UserRole.Customer);
        SetAuthHeader(customer);

        var response = await _client.DeleteAsync($"/api/vehicles/{vehicle.VehicleId}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}
