using System.Net;
using System.Net.Http.Json;
using tests.Setup;
using Backend.Features.Users;

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

        var users = await response.Content.ReadFromJsonAsync<List<UserDto>>();
        Assert.NotNull(users);
        Assert.Empty(users);
    }

    [Fact]
    public async Task CreateUser_ShouldReturnCreatedUser()
    {
        var newUser = new CreateUserDto { Name = "Fulaninho", Email = "fulano@email.com", Password = "password123", Role = "admin" };

        var response = await _client.PostAsync("/api/users", JsonContent.Create(newUser));

        response.EnsureSuccessStatusCode();
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var createdUser = await response.Content.ReadFromJsonAsync<UserDto>();
        Assert.NotNull(createdUser);
        Assert.Equal(newUser.Name, createdUser.Name);
        Assert.Equal(newUser.Email, createdUser.Email);
    }

    [Fact]
    public async Task GetUser_ShouldReturnCreatedUser()
    {
        var newUser = new CreateUserDto { Name = "Fulaninho", Email = "fulano@email.com", Password = "password123", Role = "admin" };

        var createResponse = await _client.PostAsync("/api/users", JsonContent.Create(newUser));
        createResponse.EnsureSuccessStatusCode();
        var createdUser = await createResponse.Content.ReadFromJsonAsync<UserDto>();

        var getResponse = await _client.GetAsync($"/api/users/{createdUser?.UserId}");
        getResponse.EnsureSuccessStatusCode();
        var fetchedUser = await getResponse.Content.ReadFromJsonAsync<UserDto>();

        Assert.NotNull(fetchedUser);
        Assert.Equal(createdUser?.UserId, fetchedUser.UserId);
        Assert.Equal(createdUser?.Name, fetchedUser.Name);
        Assert.Equal(createdUser?.Email, fetchedUser.Email);
    }

    [Fact]
    public async Task GetUser_WhenNotFound_ShouldReturnNotFound()
    {
        var response = await _client.GetAsync($"/api/users/{Guid.NewGuid()}");
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task CreateUser_WhenEmailAlreadyExists_ShouldReturnConflict()
    {
        var newUser = new CreateUserDto { Name = "Fulaninho", Email = "fulano@email.com", Password = "password123", Role = "admin" };
        await _client.PostAsync("/api/users", JsonContent.Create(newUser));

        var response = await _client.PostAsync("/api/users", JsonContent.Create(newUser));
        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task DeleteUser_ShouldReturnNoContent()
    {
        var newUser = new CreateUserDto { Name = "Fulaninho", Email = "fulano@email.com", Password = "password123", Role = "admin" };
        var createResponse = await _client.PostAsync("/api/users", JsonContent.Create(newUser));
        var createdUser = await createResponse.Content.ReadFromJsonAsync<UserDto>();

        var response = await _client.DeleteAsync($"/api/users/{createdUser?.UserId}");
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    [Fact]
    public async Task UpdateUser_ShouldReturnUpdatedUser()
    {
        var newUser = new CreateUserDto { Name = "Fulaninho", Email = "fulano@email.com", Password = "password123", Role = "admin" };
        var createResponse = await _client.PostAsync("/api/users", JsonContent.Create(newUser));
        var createdUser = await createResponse.Content.ReadFromJsonAsync<UserDto>();

        var updateDto = new CreateUserDto { Name = "Atualizado", Email = "atualizado@email.com", Password = "password123", Role = "admin" };
        var response = await _client.PutAsync($"/api/users/{createdUser?.UserId}", JsonContent.Create(updateDto));

        response.EnsureSuccessStatusCode();
        var updatedUser = await response.Content.ReadFromJsonAsync<UserDto>();
        Assert.Equal("Atualizado", updatedUser?.Name);
    }
}