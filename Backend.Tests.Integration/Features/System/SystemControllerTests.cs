using System.Net;
using System.Net.Http.Json;
using Backend.Features.Users;
using tests.Setup;

namespace tests.Features.SystemConfigTests;

public class SystemControllerTests(CustomWebApplicationFactory factory)
    : IClassFixture<CustomWebApplicationFactory>, IAsyncLifetime
{
    private readonly HttpClient _client = factory.CreateClient();

    public async Task InitializeAsync() => await factory.ResetDatabaseAsync();
    public Task DisposeAsync() => Task.CompletedTask;

    // GET /api/system/max-occupancy

    [Fact]
    public async Task GetMaxOccupancy_WhenNotAuthenticated_ReturnsUnauthorized()
    {
        var anonymousClient = AuthTestHelper.CreateAnonymousClient(factory);

        var response = await anonymousClient.GetAsync("/api/system/max-occupancy");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetMaxOccupancy_WhenCustomerAuthenticated_ReturnsForbidden()
    {
        var customerClient = await AuthTestHelper.CreateClientAsAsync(factory, UserRole.Customer);

        var response = await customerClient.GetAsync("/api/system/max-occupancy");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    // PUT /api/system/max-occupancy

    [Fact]
    public async Task UpdateMaxOccupancy_WhenNotAuthenticated_ReturnsUnauthorized()
    {
        var anonymousClient = AuthTestHelper.CreateAnonymousClient(factory);

        var response = await anonymousClient.PutAsJsonAsync(
            "/api/system/max-occupancy",
            new { maxOccupancy = 100 });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task UpdateMaxOccupancy_WhenCustomerAuthenticated_ReturnsForbidden()
    {
        var customerClient = await AuthTestHelper.CreateClientAsAsync(factory, UserRole.Customer);

        var response = await customerClient.PutAsJsonAsync(
            "/api/system/max-occupancy",
            new { maxOccupancy = 100 });

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }
}
