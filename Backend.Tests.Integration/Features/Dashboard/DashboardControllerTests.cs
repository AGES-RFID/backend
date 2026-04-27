using System.Net;
using System.Net.Http.Json;
using Backend.Database;
using Backend.Features.Accesses;
using Backend.Features.Dashboard;
using Backend.Features.Tags;
using Backend.Features.Tags.Enums;
using Backend.Features.Users;
using Backend.Features.Vehicles;
using Microsoft.Extensions.DependencyInjection;
using tests.Setup;

namespace tests.Features.Dashboard;

public class DashboardControllerTests(CustomWebApplicationFactory factory)
    : IClassFixture<CustomWebApplicationFactory>, IAsyncLifetime
{
    private readonly HttpClient _client = factory.CreateClient();
    private readonly IServiceScopeFactory _scopeFactory =
        factory.Services.GetRequiredService<IServiceScopeFactory>();

    public async Task InitializeAsync() => await factory.ResetDatabaseAsync();
    public Task DisposeAsync() => Task.CompletedTask;

    // ── Helpers ───────────────────────────────────────────────────────────────

    private async Task<(User user, Vehicle vehicle, Tag tag)> SeedVehicleWithTagAsync(string plate)
    {
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var user = new User
        {
            Name = "Test User",
            Email = $"user_{Guid.NewGuid()}@test.com",
            PasswordHash = "hash",
            Role = UserRole.Customer
        };
        db.Users.Add(user);

        var tag = new Tag { TagId = $"TAG-{Guid.NewGuid()}", Status = TagStatus.IN_USE };
        db.Tags.Add(tag);

        await db.SaveChangesAsync();

        var vehicle = new Vehicle
        {
            UserId = user.UserId,
            TagId = tag.TagId,
            Plate = plate,
            Brand = "Honda",
            Model = "Civic"
        };
        db.Vehicles.Add(vehicle);
        await db.SaveChangesAsync();

        return (user, vehicle, tag);
    }

    private async Task SeedAccessAsync(string tagId, AccessType type, DateTime timestamp)
    {
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var tag = await db.Tags.FindAsync(tagId)
            ?? throw new InvalidOperationException($"Tag {tagId} not found");

        db.Accesses.Add(new Access
        {
            TagId = tagId,
            Type = type,
            Timestamp = timestamp,
            Tag = tag
        });
        await db.SaveChangesAsync();
    }

    // ── Tests ─────────────────────────────────────────────────────────────────

    [Fact]
    public async Task GetOccupancy_WhenNoAccesses_ReturnsZeroOccupancy()
    {
        var response = await _client.GetAsync("/dashboard/occupancy");

        response.EnsureSuccessStatusCode();
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<OccupancyDto>(
            CustomWebApplicationFactory.JsonOptions);

        Assert.NotNull(body);
        Assert.Equal(0, body.CurrentOccupancy);
        Assert.Empty(body.Vehicles);
    }

    [Fact]
    public async Task GetOccupancy_WhenOneVehicleEntered_ReturnsOccupancyOne()
    {
        var (_, _, tag) = await SeedVehicleWithTagAsync("ENTR001");
        await SeedAccessAsync(tag.TagId, AccessType.ENTRY, DateTime.UtcNow);

        var response = await _client.GetAsync("/dashboard/occupancy");

        response.EnsureSuccessStatusCode();
        var body = await response.Content.ReadFromJsonAsync<OccupancyDto>(
            CustomWebApplicationFactory.JsonOptions);

        Assert.NotNull(body);
        Assert.Equal(1, body.CurrentOccupancy);
        Assert.Single(body.Vehicles);
        Assert.Equal("ENTR001", body.Vehicles.First().Plate);
    }

    [Fact]
    public async Task GetOccupancy_WhenVehicleExited_IsNotCountedAsInside()
    {
        var (_, _, tag) = await SeedVehicleWithTagAsync("EXIT001");
        var baseTime = DateTime.UtcNow;
        await SeedAccessAsync(tag.TagId, AccessType.ENTRY, baseTime.AddMinutes(-10));
        await SeedAccessAsync(tag.TagId, AccessType.EXIT, baseTime);

        var response = await _client.GetAsync("/dashboard/occupancy");

        response.EnsureSuccessStatusCode();
        var body = await response.Content.ReadFromJsonAsync<OccupancyDto>(
            CustomWebApplicationFactory.JsonOptions);

        Assert.NotNull(body);
        Assert.Equal(0, body.CurrentOccupancy);
        Assert.Empty(body.Vehicles);
    }

    [Fact]
    public async Task GetOccupancy_OnlyCountsVehiclesWhoseLastAccessIsEntry()
    {
        var baseTime = DateTime.UtcNow;

        // Vehicle A: entered and stayed inside
        var (_, _, tagA) = await SeedVehicleWithTagAsync("CAR-A");
        await SeedAccessAsync(tagA.TagId, AccessType.ENTRY, baseTime.AddMinutes(-30));

        // Vehicle B: entered then exited
        var (_, _, tagB) = await SeedVehicleWithTagAsync("CAR-B");
        await SeedAccessAsync(tagB.TagId, AccessType.ENTRY, baseTime.AddMinutes(-20));
        await SeedAccessAsync(tagB.TagId, AccessType.EXIT, baseTime.AddMinutes(-5));

        // Vehicle C: entered, exited, entered again (back inside)
        var (_, _, tagC) = await SeedVehicleWithTagAsync("CAR-C");
        await SeedAccessAsync(tagC.TagId, AccessType.ENTRY, baseTime.AddMinutes(-60));
        await SeedAccessAsync(tagC.TagId, AccessType.EXIT, baseTime.AddMinutes(-40));
        await SeedAccessAsync(tagC.TagId, AccessType.ENTRY, baseTime.AddMinutes(-2));

        var response = await _client.GetAsync("/dashboard/occupancy");

        response.EnsureSuccessStatusCode();
        var body = await response.Content.ReadFromJsonAsync<OccupancyDto>(
            CustomWebApplicationFactory.JsonOptions);

        Assert.NotNull(body);
        Assert.Equal(2, body.CurrentOccupancy); // A and C
        Assert.Equal(2, body.Vehicles.Count());

        var plates = body.Vehicles.Select(v => v.Plate).ToHashSet();
        Assert.Contains("CAR-A", plates);
        Assert.Contains("CAR-C", plates);
        Assert.DoesNotContain("CAR-B", plates);
    }

    [Fact]
    public async Task GetOccupancy_ReturnsOkStatusCode()
    {
        var response = await _client.GetAsync("/dashboard/occupancy");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }
}
