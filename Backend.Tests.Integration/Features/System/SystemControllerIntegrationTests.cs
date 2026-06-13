using System.Net;
using System.Net.Http.Json;
using Backend.Features.SystemConfig;
using Backend.Features.Users;
using tests.Setup;

namespace tests.Features.SystemConfigTests;

public class SystemControllerIntegrationTests(CustomWebApplicationFactory factory)
    : IClassFixture<CustomWebApplicationFactory>, IAsyncLifetime
{
    public async Task InitializeAsync() => await factory.ResetDatabaseAsync();
    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task GetSystem_WhenAdmin_ReturnsSystemDtoWithAntennas()
    {
        var adminClient = await AuthTestHelper.CreateClientAsAsync(factory, UserRole.Admin);
        var response = await adminClient.GetAsync("/api/system");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var json = await response.Content.ReadAsStringAsync();
        using var doc = System.Text.Json.JsonDocument.Parse(json);
        var root = doc.RootElement;

        Assert.True(root.TryGetProperty("occupancyLimit", out var _));
        Assert.True(root.TryGetProperty("currentOccupancy", out var _));
        Assert.True(root.TryGetProperty("antennas", out var antennas));
        Assert.True(antennas.ValueKind == System.Text.Json.JsonValueKind.Array);
    }
}
