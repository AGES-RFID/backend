using System.Net;
using System.Net.Http.Json;
using Backend.Database;
using Backend.Features.Settings;
using Backend.Features.SystemConfig;
using Backend.Features.Users;
using Microsoft.Extensions.DependencyInjection;
using tests.Setup;

namespace tests.Features.SystemConfigTests;

public class SystemControllerTests(CustomWebApplicationFactory factory)
    : IClassFixture<CustomWebApplicationFactory>, IAsyncLifetime
{
    private readonly IServiceScopeFactory _scopeFactory =
        factory.Services.GetRequiredService<IServiceScopeFactory>();

    public async Task InitializeAsync() => await factory.ResetDatabaseAsync();
    public Task DisposeAsync() => Task.CompletedTask;

    private async Task SeedSettingAsync(string name, string value)
    {
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        db.Settings.Add(new Settings { Name = name, Value = value });
        await db.SaveChangesAsync();
    }

    // GET /api/system/occupancy-limit

    [Fact]
    public async Task GetOccupancyLimit_WhenSettingExists_ReturnsValue()
    {
        await SeedSettingAsync("max_occupancy", "120");
        var adminClient = await AuthTestHelper.CreateClientAsAsync(factory, UserRole.Admin);

        var response = await adminClient.GetAsync("/api/system/occupancy-limit");

        response.EnsureSuccessStatusCode();
        var body = await response.Content.ReadFromJsonAsync<OccupancyLimitDto>(
            CustomWebApplicationFactory.JsonOptions);
        Assert.NotNull(body);
        Assert.Equal(120, body.MaxOccupancy);
    }

    [Fact]
    public async Task GetOccupancyLimit_WhenNoSetting_ReturnsDefault100()
    {
        var adminClient = await AuthTestHelper.CreateClientAsAsync(factory, UserRole.Admin);

        var response = await adminClient.GetAsync("/api/system/occupancy-limit");

        response.EnsureSuccessStatusCode();
        var body = await response.Content.ReadFromJsonAsync<OccupancyLimitDto>(
            CustomWebApplicationFactory.JsonOptions);
        Assert.NotNull(body);
        Assert.Equal(100, body.MaxOccupancy);
    }

    [Fact]
    public async Task GetOccupancyLimit_ReturnsStatusCode200()
    {
        var adminClient = await AuthTestHelper.CreateClientAsAsync(factory, UserRole.Admin);

        var response = await adminClient.GetAsync("/api/system/occupancy-limit");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task GetOccupancyLimit_WhenNotAuthenticated_ReturnsUnauthorized()
    {
        var anonymousClient = AuthTestHelper.CreateAnonymousClient(factory);

        var response = await anonymousClient.GetAsync("/api/system/occupancy-limit");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetOccupancyLimit_WhenCustomerAuthenticated_ReturnsForbidden()
    {
        var customerClient = await AuthTestHelper.CreateClientAsAsync(factory, UserRole.Customer);

        var response = await customerClient.GetAsync("/api/system/occupancy-limit");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    // PUT /api/system/occupancy-limit

    [Fact]
    public async Task UpdateOccupancyLimit_WhenValidInput_ReturnsUpdatedValue()
    {
        var adminClient = await AuthTestHelper.CreateClientAsAsync(factory, UserRole.Admin);

        var response = await adminClient.PutAsJsonAsync(
            "/api/system/occupancy-limit",
            new UpdateOccupancyLimitDto { MaxOccupancy = 150 });

        response.EnsureSuccessStatusCode();
        var body = await response.Content.ReadFromJsonAsync<OccupancyLimitDto>(
            CustomWebApplicationFactory.JsonOptions);
        Assert.NotNull(body);
        Assert.Equal(150, body.MaxOccupancy);
    }

    [Fact]
    public async Task UpdateOccupancyLimit_WhenSettingAlreadyExists_OverwritesValue()
    {
        await SeedSettingAsync("max_occupancy", "100");
        var adminClient = await AuthTestHelper.CreateClientAsAsync(factory, UserRole.Admin);

        await adminClient.PutAsJsonAsync(
            "/api/system/occupancy-limit",
            new UpdateOccupancyLimitDto { MaxOccupancy = 250 });

        var getResponse = await adminClient.GetAsync("/api/system/occupancy-limit");
        var body = await getResponse.Content.ReadFromJsonAsync<OccupancyLimitDto>(
            CustomWebApplicationFactory.JsonOptions);
        Assert.NotNull(body);
        Assert.Equal(250, body.MaxOccupancy);
    }

    [Fact]
    public async Task UpdateOccupancyLimit_ReturnsStatusCode200()
    {
        var adminClient = await AuthTestHelper.CreateClientAsAsync(factory, UserRole.Admin);

        var response = await adminClient.PutAsJsonAsync(
            "/api/system/occupancy-limit",
            new UpdateOccupancyLimitDto { MaxOccupancy = 100 });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task UpdateOccupancyLimit_WhenNotAuthenticated_ReturnsUnauthorized()
    {
        var anonymousClient = AuthTestHelper.CreateAnonymousClient(factory);

        var response = await anonymousClient.PutAsJsonAsync(
            "/api/system/occupancy-limit",
            new UpdateOccupancyLimitDto { MaxOccupancy = 100 });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task UpdateOccupancyLimit_WhenCustomerAuthenticated_ReturnsForbidden()
    {
        var customerClient = await AuthTestHelper.CreateClientAsAsync(factory, UserRole.Customer);

        var response = await customerClient.PutAsJsonAsync(
            "/api/system/occupancy-limit",
            new UpdateOccupancyLimitDto { MaxOccupancy = 100 });

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }
}
