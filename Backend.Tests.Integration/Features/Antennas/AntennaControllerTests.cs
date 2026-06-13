using System.Net;
using System.Net.Http.Json;
using Backend.Features.Antennas;
using Backend.Features.Users;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using NSubstitute;
using tests.Setup;

namespace tests.Features.Antennas;

public class AntennaControllerTests : IClassFixture<CustomWebApplicationFactory>, IAsyncLifetime
{
    private readonly CustomWebApplicationFactory _factory;
    private readonly IGatewayClient _gatewayClient = Substitute.For<IGatewayClient>();

    #pragma warning disable CA2213
    private readonly HttpClient _warmupClient;
    #pragma warning restore CA2213

    public AntennaControllerTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
        _warmupClient = factory.CreateClient();
        _gatewayClient.GetAntennasAsync().Returns(new List<AntennaDto>());
        _gatewayClient.GetAntennaAsync(Arg.Any<Guid>()).Returns(new AntennaDto { Id = Guid.NewGuid(), Number = 1, Status = "On" });
        _gatewayClient.UpdateAntennaAsync(Arg.Any<Guid>(), Arg.Any<UpdateAntennaDto>())
            .Returns(new AntennaDto { Id = Guid.NewGuid(), Number = 1, Status = "On" });
    }

    private HttpClient CreateClientWithGatewayMock()
    {
        return _factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureTestServices(services =>
            {
                services.RemoveAll<IGatewayClient>();
                services.AddSingleton(_gatewayClient);
            });
        }).CreateClient();
    }

    private async Task<HttpClient> CreateAdminClientWithGatewayMockAsync()
    {
        var scopeFactory = _factory.Services.GetRequiredService<IServiceScopeFactory>();
        using var scope = scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<Backend.Database.AppDbContext>();
        var user = new User
        {
            Name = $"Admin-{Guid.NewGuid()}",
            Email = $"admin_{Guid.NewGuid()}@test.com",
            PasswordHash = "hash",
            Role = UserRole.Admin
        };
        db.Users.Add(user);
        await db.SaveChangesAsync();

        var client = CreateClientWithGatewayMock();
        client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", AuthTestHelper.CreateTokenForUser(user));
        return client;
    }

    public async Task InitializeAsync() => await _factory.ResetDatabaseAsync();
    public Task DisposeAsync() => Task.CompletedTask;

    // --- GET /api/antennas ---

    [Fact]
    public async Task GetAntennas_WhenNotAuthenticated_ReturnsUnauthorized()
    {
        var response = await CreateClientWithGatewayMock().GetAsync("/api/antennas");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetAntennas_WhenCustomerAuthenticated_ReturnsForbidden()
    {
        var client = await AuthTestHelper.CreateClientAsAsync(_factory, UserRole.Customer);
        var response = await client.GetAsync("/api/antennas");
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task GetAntennas_WhenAdminAuthenticated_ReturnsOk()
    {
        var client = await CreateAdminClientWithGatewayMockAsync();
        var response = await client.GetAsync("/api/antennas");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    // --- GET /api/antennas/{id} ---

    [Fact]
    public async Task GetAntenna_WhenNotAuthenticated_ReturnsUnauthorized()
    {
        var response = await CreateClientWithGatewayMock().GetAsync($"/api/antennas/{Guid.NewGuid()}");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetAntenna_WhenCustomerAuthenticated_ReturnsForbidden()
    {
        var client = await AuthTestHelper.CreateClientAsAsync(_factory, UserRole.Customer);
        var response = await client.GetAsync($"/api/antennas/{Guid.NewGuid()}");
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    // --- PUT /api/antennas/{id} ---

    [Fact]
    public async Task UpdateAntenna_WhenNotAuthenticated_ReturnsUnauthorized()
    {
        var response = await CreateClientWithGatewayMock().PutAsJsonAsync(
            $"/api/antennas/{Guid.NewGuid()}",
            new UpdateAntennaDto { Status = "On" });
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task UpdateAntenna_WhenCustomerAuthenticated_ReturnsForbidden()
    {
        var client = await AuthTestHelper.CreateClientAsAsync(_factory, UserRole.Customer);
        var response = await client.PutAsJsonAsync(
            $"/api/antennas/{Guid.NewGuid()}",
            new UpdateAntennaDto { Status = "On" });
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }
}
