using System.Net;
using System.Net.Http.Json;
using Backend.Database;
using Backend.Features.Accesses;
using Backend.Features.Dashboard;
using Backend.Features.Tags;
using Backend.Features.Tags.Enums;
using Backend.Features.Users;
using Backend.Features.Vehicles;
using Backend.Features.Settings;
using Microsoft.Extensions.DependencyInjection;
using tests.Setup;

namespace tests.Features.Dashboard;

public class DashboardControllerTests(CustomWebApplicationFactory factory)
    : IClassFixture<CustomWebApplicationFactory>, IAsyncLifetime
{
    private readonly IServiceScopeFactory _scopeFactory =
        factory.Services.GetRequiredService<IServiceScopeFactory>();

    public async Task InitializeAsync() => await factory.ResetDatabaseAsync();
    public Task DisposeAsync() => Task.CompletedTask;

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

        var tag = new Tag { Status = TagStatus.IN_USE, Epc = $"EPC-{Guid.NewGuid()}", Tid = $"TID-{Guid.NewGuid()}" };
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

    private async Task SeedAccessAsync(Guid tagId, AccessType type, DateTime timestamp)
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

    [Fact]
    public async Task GetOccupancy_WhenNoAccesses_ReturnsZeroOccupancy()
    {
        var adminClient = await AuthTestHelper.CreateClientAsAsync(factory, UserRole.Admin);
        var response = await adminClient.GetAsync("/api/dashboard/occupancy");

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
        var adminClient = await AuthTestHelper.CreateClientAsAsync(factory, UserRole.Admin);
        var (_, _, tag) = await SeedVehicleWithTagAsync("ENTR001");
        await SeedAccessAsync(tag.TagId, AccessType.Entry, DateTime.UtcNow);

        var response = await adminClient.GetAsync("/api/dashboard/occupancy");

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
        var adminClient = await AuthTestHelper.CreateClientAsAsync(factory, UserRole.Admin);
        var (_, _, tag) = await SeedVehicleWithTagAsync("EXIT001");
        var baseTime = DateTime.UtcNow;
        await SeedAccessAsync(tag.TagId, AccessType.Entry, baseTime.AddMinutes(-10));
        await SeedAccessAsync(tag.TagId, AccessType.Exit, baseTime);

        var response = await adminClient.GetAsync("/api/dashboard/occupancy");

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
        var adminClient = await AuthTestHelper.CreateClientAsAsync(factory, UserRole.Admin);
        var baseTime = DateTime.UtcNow;
        //Access flow(Entry)
        var (_, _, tagA) = await SeedVehicleWithTagAsync("CAR-A");
        await SeedAccessAsync(tagA.TagId, AccessType.Entry, baseTime.AddMinutes(-30));
        //Access flow(Entry-Exit)
        var (_, _, tagB) = await SeedVehicleWithTagAsync("CAR-B");
        await SeedAccessAsync(tagB.TagId, AccessType.Entry, baseTime.AddMinutes(-20));
        await SeedAccessAsync(tagB.TagId, AccessType.Exit, baseTime.AddMinutes(-5));
        //Access flow(Entry-Exit-Entry)
        var (_, _, tagC) = await SeedVehicleWithTagAsync("CAR-C");
        await SeedAccessAsync(tagC.TagId, AccessType.Entry, baseTime.AddMinutes(-60));
        await SeedAccessAsync(tagC.TagId, AccessType.Exit, baseTime.AddMinutes(-40));
        await SeedAccessAsync(tagC.TagId, AccessType.Entry, baseTime.AddMinutes(-2));

        var response = await adminClient.GetAsync("/api/dashboard/occupancy");

        response.EnsureSuccessStatusCode();
        var body = await response.Content.ReadFromJsonAsync<OccupancyDto>(
            CustomWebApplicationFactory.JsonOptions);

        Assert.NotNull(body);
        Assert.Equal(2, body.CurrentOccupancy);
        Assert.Equal(2, body.Vehicles.Count());

        var plates = body.Vehicles.Select(v => v.Plate).ToHashSet();
        Assert.Contains("CAR-A", plates);
        Assert.Contains("CAR-C", plates);
        Assert.DoesNotContain("CAR-B", plates);
    }

    [Fact]
    public async Task GetOccupancy_ReturnsOkStatusCode()
    {
        var adminClient = await AuthTestHelper.CreateClientAsAsync(factory, UserRole.Admin);
        var response = await adminClient.GetAsync("/api/dashboard/occupancy");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task GetMetrics_ShouldReturnOk()
    {
        var adminClient = await AuthTestHelper.CreateClientAsAsync(factory, UserRole.Admin);
        var response = await adminClient.GetAsync("/api/dashboard/metrics");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task GetMetrics_WhenNoAccesses_ShouldReturnZerosAndNullPeakTime()
    {
        var adminClient = await AuthTestHelper.CreateClientAsAsync(factory, UserRole.Admin);
        var response = await adminClient.GetAsync("/api/dashboard/metrics");
        response.EnsureSuccessStatusCode();

        var metrics = await response.Content.ReadFromJsonAsync<DashboardMetricsDto>(CustomWebApplicationFactory.JsonOptions);

        Assert.NotNull(metrics);
        Assert.Equal(0, metrics.EntriesLastHour);
        Assert.Equal(0, metrics.ExitsLastHour);
        Assert.Null(metrics.PeakEntryTime);
        Assert.Equal(0, metrics.PeakHourEntries);
    }

    [Fact]
    public async Task GetMetrics_ShouldReturnCorrectFields()
    {
        var adminClient = await AuthTestHelper.CreateClientAsAsync(factory, UserRole.Admin);
        var response = await adminClient.GetAsync("/api/dashboard/metrics");
        response.EnsureSuccessStatusCode();

        var metrics = await response.Content.ReadFromJsonAsync<DashboardMetricsDto>(CustomWebApplicationFactory.JsonOptions);

        Assert.NotNull(metrics);
        Assert.True(metrics.EntriesLastHour >= 0);
        Assert.True(metrics.ExitsLastHour >= 0);
    }

    [Fact]
    public async Task GetOccupancy_WhenNotAuthenticated_ReturnsUnauthorized()
    {
        var anonymousClient = AuthTestHelper.CreateAnonymousClient(factory);

        var response = await anonymousClient.GetAsync("/api/dashboard/occupancy");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetMetrics_WhenNotAuthenticated_ReturnsUnauthorized()
    {
        var anonymousClient = AuthTestHelper.CreateAnonymousClient(factory);

        var response = await anonymousClient.GetAsync("/api/dashboard/metrics");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetOccupancy_WhenCustomerAuthenticated_ReturnsForbidden()
    {
        var customerClient = await AuthTestHelper.CreateClientAsAsync(factory, UserRole.Customer);

        var response = await customerClient.GetAsync("/api/dashboard/occupancy");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task GetMetrics_WhenCustomerAuthenticated_ReturnsForbidden()
    {
        var customerClient = await AuthTestHelper.CreateClientAsAsync(factory, UserRole.Customer);

        var response = await customerClient.GetAsync("/api/dashboard/metrics");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task GetMetrics_WhenSettingsExist_ReturnsMaxOccupancy()
    {
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        db.Settings.Add(new Settings { Name = "max_occupancy", Value = "200" });
        await db.SaveChangesAsync();

        var adminClient = await AuthTestHelper.CreateClientAsAsync(factory, UserRole.Admin);
        var response = await adminClient.GetAsync("/api/dashboard/metrics");
        response.EnsureSuccessStatusCode();

        var metrics = await response.Content.ReadFromJsonAsync<DashboardMetricsDto>(CustomWebApplicationFactory.JsonOptions);
        Assert.NotNull(metrics);
        Assert.Equal(200, metrics.MaxOccupancy);
    }

    [Fact]
    public async Task GetMetrics_WhenNoSettings_ReturnsDefaultMaxOccupancy()
    {
        var adminClient = await AuthTestHelper.CreateClientAsAsync(factory, UserRole.Admin);
        var response = await adminClient.GetAsync("/api/dashboard/metrics");
        response.EnsureSuccessStatusCode();

        var metrics = await response.Content.ReadFromJsonAsync<DashboardMetricsDto>(CustomWebApplicationFactory.JsonOptions);
        Assert.NotNull(metrics);
        Assert.Equal(100, metrics.MaxOccupancy);
    }

    [Fact]
    public async Task GetOccupancy_WhenSettingsExist_ReturnsMaxOccupancy()
    {
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        db.Settings.Add(new Settings { Name = "max_occupancy", Value = "150" });
        await db.SaveChangesAsync();

        var adminClient = await AuthTestHelper.CreateClientAsAsync(factory, UserRole.Admin);
        var response = await adminClient.GetAsync("/api/dashboard/occupancy");
        response.EnsureSuccessStatusCode();

        var body = await response.Content.ReadFromJsonAsync<OccupancyDto>(CustomWebApplicationFactory.JsonOptions);
        Assert.NotNull(body);
        Assert.Equal(150, body.MaxOccupancy);
    }

    [Fact]
    public async Task GetOccupancy_WhenNoSettings_ReturnsDefaultMaxOccupancy()
    {
        var adminClient = await AuthTestHelper.CreateClientAsAsync(factory, UserRole.Admin);
        var response = await adminClient.GetAsync("/api/dashboard/occupancy");
        response.EnsureSuccessStatusCode();

        var body = await response.Content.ReadFromJsonAsync<OccupancyDto>(CustomWebApplicationFactory.JsonOptions);
        Assert.NotNull(body);
        Assert.Equal(100, body.MaxOccupancy);
    }

    [Fact]
    public async Task GetOccupancy_WhenOneVehicleInsideAndMaxIs100_ReturnsOnePercentOccupancy()
    {
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        db.Settings.Add(new Settings { Name = "max_occupancy", Value = "100" });
        await db.SaveChangesAsync();

        var adminClient = await AuthTestHelper.CreateClientAsAsync(factory, UserRole.Admin);
        var (_, _, tag) = await SeedVehicleWithTagAsync("PCT-001");
        await SeedAccessAsync(tag.TagId, AccessType.Entry, DateTime.UtcNow);

        var response = await adminClient.GetAsync("/api/dashboard/occupancy");
        response.EnsureSuccessStatusCode();

        var body = await response.Content.ReadFromJsonAsync<OccupancyDto>(CustomWebApplicationFactory.JsonOptions);
        Assert.NotNull(body);
        Assert.Equal(1.0, body.OccupancyPercentage);
    }

    [Fact]
    public async Task GetMetrics_WithEntriesAndExitsInLastHour_ReturnsCalculatedCounts()
    {
        var adminClient = await AuthTestHelper.CreateClientAsAsync(factory, UserRole.Admin);
        var baseTime = DateTime.UtcNow;

        var (_, _, entryTag1) = await SeedVehicleWithTagAsync("ENT-001");
        var (_, _, entryTag2) = await SeedVehicleWithTagAsync("ENT-002");
        var (_, _, exitTag) = await SeedVehicleWithTagAsync("EXT-001");

        await SeedAccessAsync(entryTag1.TagId, AccessType.Entry, baseTime.AddMinutes(-45));
        await SeedAccessAsync(entryTag2.TagId, AccessType.Entry, baseTime.AddMinutes(-20));
        await SeedAccessAsync(exitTag.TagId, AccessType.Exit, baseTime.AddMinutes(-10));

        await SeedAccessAsync(entryTag1.TagId, AccessType.Entry, baseTime.AddHours(-2));

        var response = await adminClient.GetAsync("/api/dashboard/metrics");

        response.EnsureSuccessStatusCode();
        var metrics = await response.Content.ReadFromJsonAsync<DashboardMetricsDto>(CustomWebApplicationFactory.JsonOptions);

        Assert.NotNull(metrics);
        Assert.Equal(2, metrics.EntriesLastHour);
        Assert.Equal(1, metrics.ExitsLastHour);
    }

    [Fact]
    public async Task GetMetrics_WithAccessesInPast24Hours_ReturnsPeakEntryTime()
    {
        var adminClient = await AuthTestHelper.CreateClientAsAsync(factory, UserRole.Admin);
        var now = DateTime.UtcNow;
        var peakHour = new DateTime(now.Year, now.Month, now.Day, now.Hour, 0, 0, DateTimeKind.Utc).AddHours(-5);
        var nonPeakHour = peakHour.AddHours(-5);

        var (_, _, vehicleA) = await SeedVehicleWithTagAsync("PEAK-A");
        var (_, _, vehicleB) = await SeedVehicleWithTagAsync("PEAK-B");

        await SeedAccessAsync(vehicleA.TagId, AccessType.Entry, peakHour.AddMinutes(10));
        await SeedAccessAsync(vehicleB.TagId, AccessType.Entry, peakHour.AddMinutes(20));
        await SeedAccessAsync(vehicleA.TagId, AccessType.Entry, peakHour.AddMinutes(30));

        await SeedAccessAsync(vehicleA.TagId, AccessType.Entry, nonPeakHour.AddMinutes(15));

        var response = await adminClient.GetAsync("/api/dashboard/metrics");

        response.EnsureSuccessStatusCode();
        var metrics = await response.Content.ReadFromJsonAsync<DashboardMetricsDto>(CustomWebApplicationFactory.JsonOptions);

        Assert.NotNull(metrics);
        Assert.Equal($"{peakHour.Hour:D2}:00", metrics.PeakEntryTime);
    }

    [Fact]
    public async Task GetDashboard_WhenCalled_ReturnsCombinedMetricsAndOccupancy()
    {
        var adminClient = await AuthTestHelper.CreateClientAsAsync(factory, UserRole.Admin);
        var baseTime = DateTime.UtcNow;
        var (_, _, vehicle) = await SeedVehicleWithTagAsync("AAA-999");

        await SeedAccessAsync(vehicle.TagId, AccessType.Entry, baseTime.AddMinutes(-30));

        var response = await adminClient.GetAsync("/api/dashboard");

        response.EnsureSuccessStatusCode();
        var metrics = await response.Content.ReadFromJsonAsync<DashboardMetricsDto>(CustomWebApplicationFactory.JsonOptions);

        Assert.NotNull(metrics);
        Assert.Equal(1, metrics.EntriesLastHour);
        Assert.Equal(0, metrics.ExitsLastHour);
        Assert.Equal(1, metrics.CurrentOccupancy);
        Assert.Equal(100, metrics.MaxOccupancy);
        Assert.Equal(1, metrics.PeakHourEntries);
        Assert.Equal($"{baseTime.AddMinutes(-30).Hour:D2}:00", metrics.PeakEntryTime);
        Assert.True(metrics.UpdatedAt > DateTime.MinValue);
        Assert.NotNull(metrics.Accesses);
        Assert.True(metrics.PeakHourEntries > 0);
        Assert.True(metrics.UpdatedAt > DateTime.MinValue);
        Assert.NotEmpty(metrics.Accesses);
    }
}