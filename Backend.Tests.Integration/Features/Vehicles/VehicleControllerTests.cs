using System.Net;
using System.Net.Http.Json;
using Backend.Database;
using Backend.Features.Users;
using Backend.Features.Vehicles;
using Microsoft.Extensions.DependencyInjection;
using tests.Setup;

namespace tests.Features.Vehicles;

public class VehicleControllerTests(CustomWebApplicationFactory factory) : IClassFixture<CustomWebApplicationFactory>, IAsyncLifetime
{
    private readonly HttpClient _client = factory.CreateClient();
    private readonly IServiceScopeFactory _scopeFactory = factory.Services.GetRequiredService<IServiceScopeFactory>();

    public async Task InitializeAsync()
    {
        await factory.ResetDatabaseAsync();
    }

    public Task DisposeAsync() => Task.CompletedTask;

    private async Task<User> SeedUserAsync()
    {
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var user = new User
        {
            Name = "Test Owner",
            Email = $"test_{Guid.NewGuid()}@email.com",
            PasswordHash = "dummy_hash",
            Role = UserRole.Admin
        };

        db.Users.Add(user);
        await db.SaveChangesAsync();
        return user;
    }

    [Fact]
    public async Task CreateVehicle_WithValidPayload_ReturnsCreated()
    {
        var user = await SeedUserAsync();
        var payload = new CreateVehicleDto { Brand = "Honda", Model = "HRV", Plate = "AAA9A99", UserId = user.UserId };

        var response = await _client.PostAsync("/api/vehicles", JsonContent.Create(payload));

        response.EnsureSuccessStatusCode();
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var created = await response.Content.ReadFromJsonAsync<VehicleDto>();
        Assert.NotNull(created);
        Assert.Equal(payload.Plate, created.Plate);
        Assert.Equal(payload.UserId, created.UserId);
        Assert.NotEqual(Guid.Empty, created.VehicleId);
    }

    [Fact]
    public async Task CreateVehicle_WithExistingPlate_ReturnsConflict()
    {
        var user = await SeedUserAsync();
        var payload = new CreateVehicleDto { Brand = "Honda", Model = "HRV", Plate = "SAMEPLT", UserId = user.UserId };

        await _client.PostAsync("/api/vehicles", JsonContent.Create(payload));

        var response = await _client.PostAsync("/api/vehicles", JsonContent.Create(payload));

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task CreateVehicle_WithNonExistentUserId_ReturnsNotFound()
    {
        var payload = new CreateVehicleDto { Brand = "Honda", Model = "HRV", Plate = "AAA9A99", UserId = Guid.NewGuid() };

        var response = await _client.PostAsync("/api/vehicles", JsonContent.Create(payload));

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task UpdateVehicle_WithValidData_ReturnsOk()
    {
        var user = await SeedUserAsync();
        var createPayload = new CreateVehicleDto { Brand = "Honda", Model = "HRV", Plate = "UPDT001", UserId = user.UserId };

        var createResponse = await _client.PostAsync("/api/vehicles", JsonContent.Create(createPayload));
        var created = await createResponse.Content.ReadFromJsonAsync<VehicleDto>();

        var updatePayload = new CreateVehicleDto { Brand = "Toyota", Model = "Corolla", Plate = "UPDT002", UserId = user.UserId };

        var updateResponse = await _client.PutAsync($"/api/vehicles/{created!.VehicleId}", JsonContent.Create(updatePayload));

        updateResponse.EnsureSuccessStatusCode();
        var updated = await updateResponse.Content.ReadFromJsonAsync<VehicleDto>();

        Assert.NotNull(updated);
        Assert.Equal("Toyota", updated.Brand);
        Assert.Equal("UPDT002", updated.Plate);
    }

    [Fact]
    public async Task GetVehicleById_WithExistingId_ReturnsOk()
    {
        var user = await SeedUserAsync();
        var payload = new CreateVehicleDto { Brand = "Honda", Model = "HRV", Plate = "GET0001", UserId = user.UserId };

        var createResponse = await _client.PostAsync("/api/vehicles", JsonContent.Create(payload));
        var created = await createResponse.Content.ReadFromJsonAsync<VehicleDto>();

        var response = await _client.GetAsync($"/api/vehicles/{created!.VehicleId}");

        response.EnsureSuccessStatusCode();
        var fetched = await response.Content.ReadFromJsonAsync<VehicleDto>();
        Assert.NotNull(fetched);
        Assert.Equal(created.VehicleId, fetched.VehicleId);
    }

    [Fact]
    public async Task GetAllVehicles_ReturnsOkWithList()
    {
        var user = await SeedUserAsync();
        var payload = new CreateVehicleDto { Brand = "Honda", Model = "HRV", Plate = "LIST001", UserId = user.UserId };

        await _client.PostAsync("/api/vehicles", JsonContent.Create(payload));

        var response = await _client.GetAsync("/api/vehicles");

        response.EnsureSuccessStatusCode();
        var fetched = await response.Content.ReadFromJsonAsync<List<VehicleDto>>();
        Assert.NotNull(fetched);
        Assert.NotEmpty(fetched);
    }

    [Fact]
    public async Task DeleteVehicle_WithValidId_ReturnsNoContent()
    {
        var user = await SeedUserAsync();
        var payload = new CreateVehicleDto { Brand = "Honda", Model = "HRV", Plate = "DEL0001", UserId = user.UserId };

        var createResponse = await _client.PostAsync("/api/vehicles", JsonContent.Create(payload));
        var created = await createResponse.Content.ReadFromJsonAsync<VehicleDto>();

        var delResponse = await _client.DeleteAsync($"/api/vehicles/{created!.VehicleId}");

        delResponse.EnsureSuccessStatusCode();
        Assert.Equal(HttpStatusCode.NoContent, delResponse.StatusCode);

        var getResponse = await _client.GetAsync($"/api/vehicles/{created.VehicleId}");
        Assert.Equal(HttpStatusCode.NotFound, getResponse.StatusCode);
    }
}
