using System.Net;
using System.Net.Http.Json;
using tests.Setup;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;

using Backend.Features.Users;
using Backend.Database;

namespace tests.Features.Users;

public class UserControllerTests(CustomWebApplicationFactory factory) : IClassFixture<CustomWebApplicationFactory>, IAsyncLifetime
{
    private readonly HttpClient _client = factory.CreateClient();

    public async Task InitializeAsync()
    {
        await factory.ResetDatabaseAsync();
    }

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task GetUsers_ShouldReturnSuccess()
    {
        var response = await _client.GetAsync("/api/users");

        response.EnsureSuccessStatusCode();
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var users = await response.Content.ReadFromJsonAsync<List<User>>();
        Assert.NotNull(users);
        Assert.Empty(users);
    }

}