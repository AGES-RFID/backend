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

        var users = await response.Content.ReadFromJsonAsync<List<UserDto>>(CustomWebApplicationFactory.JsonOptions);
        Assert.NotNull(users);
        Assert.Empty(users);
    }

     [Fact]
     public async Task CreateUser_ShouldReturnCreatedUser()
     {
         var newUser = new CreateUserDto { Name = "Fulaninho", Email = "fulano@email.com", Password = "password123", Role = UserRole.Admin };

         var response = await _client.PostAsync("/api/users", JsonContent.Create(newUser, options: CustomWebApplicationFactory.JsonOptions));

         response.EnsureSuccessStatusCode();
         Assert.Equal(HttpStatusCode.Created, response.StatusCode);

         var createdUser = await response.Content.ReadFromJsonAsync<UserDto>(CustomWebApplicationFactory.JsonOptions);
         Assert.NotNull(createdUser);
         Assert.Equal(newUser.Name, createdUser.Name);
         Assert.Equal(newUser.Email, createdUser.Email);
     }

     [Fact]
     public async Task GetUser_ShouldReturnCreatedUser()
     {
         var newUser = new CreateUserDto { Name = "Fulaninho", Email = "fulano@email.com", Password = "password123", Role = UserRole.Admin };

         var createResponse = await _client.PostAsync("/api/users", JsonContent.Create(newUser, options: CustomWebApplicationFactory.JsonOptions));
         createResponse.EnsureSuccessStatusCode();
         var createdUser = await createResponse.Content.ReadFromJsonAsync<UserDto>(CustomWebApplicationFactory.JsonOptions);

         var getResponse = await _client.GetAsync($"/api/users/{createdUser?.UserId}");
         getResponse.EnsureSuccessStatusCode();
         var fetchedUser = await getResponse.Content.ReadFromJsonAsync<UserDto>(CustomWebApplicationFactory.JsonOptions);

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
         var newUser = new CreateUserDto { Name = "Fulaninho", Email = "fulano@email.com", Password = "password123", Role = UserRole.Admin };
         await _client.PostAsync("/api/users", JsonContent.Create(newUser, options: CustomWebApplicationFactory.JsonOptions));

         var response = await _client.PostAsync("/api/users", JsonContent.Create(newUser, options: CustomWebApplicationFactory.JsonOptions));
         Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
     }

     [Fact]
     public async Task DeleteUser_ShouldReturnNoContent()
     {
         var newUser = new CreateUserDto { Name = "Fulaninho", Email = "fulano@email.com", Password = "password123", Role = UserRole.Admin };
         var createResponse = await _client.PostAsync("/api/users", JsonContent.Create(newUser, options: CustomWebApplicationFactory.JsonOptions));
         var createdUser = await createResponse.Content.ReadFromJsonAsync<UserDto>(CustomWebApplicationFactory.JsonOptions);

         var response = await _client.DeleteAsync($"/api/users/{createdUser?.UserId}");
         Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
     }

     [Fact]
     public async Task UpdateUser_ShouldReturnUpdatedUser()
     {
         var newUser = new CreateUserDto { Name = "Fulaninho", Email = "fulano@email.com", Password = "password123", Role = UserRole.Admin };
         var createResponse = await _client.PostAsync("/api/users", JsonContent.Create(newUser, options: CustomWebApplicationFactory.JsonOptions));
         var createdUser = await createResponse.Content.ReadFromJsonAsync<UserDto>(CustomWebApplicationFactory.JsonOptions);

         var updateDto = new UpdateUserDto { Name = "Atualizado", Email = "atualizado@email.com" };
         var response = await _client.PatchAsync($"/api/users/{createdUser?.UserId}", JsonContent.Create(updateDto, options: CustomWebApplicationFactory.JsonOptions));

         response.EnsureSuccessStatusCode();
         var updatedUser = await response.Content.ReadFromJsonAsync<UserDto>(CustomWebApplicationFactory.JsonOptions);
         Assert.Equal("Atualizado", updatedUser?.Name);
     }

     [Fact]
     public async Task UpdateUser_WhenUpdatingOnlyName_ShouldUpdateNameOnly()
     {
         var newUser = new CreateUserDto { Name = "Original", Email = "original@email.com", Password = "password123", Role = UserRole.Admin };
         var createResponse = await _client.PostAsync("/api/users", JsonContent.Create(newUser, options: CustomWebApplicationFactory.JsonOptions));
         var createdUser = await createResponse.Content.ReadFromJsonAsync<UserDto>(CustomWebApplicationFactory.JsonOptions);

         var updateDto = new UpdateUserDto { Name = "Updated" };
         var response = await _client.PatchAsync($"/api/users/{createdUser?.UserId}", JsonContent.Create(updateDto, options: CustomWebApplicationFactory.JsonOptions));

         response.EnsureSuccessStatusCode();
         var updatedUser = await response.Content.ReadFromJsonAsync<UserDto>(CustomWebApplicationFactory.JsonOptions);
         Assert.Equal("Updated", updatedUser?.Name);
         Assert.Equal("original@email.com", updatedUser?.Email);
     }

     [Fact]
     public async Task UpdateUser_WhenUpdatingOnlyEmail_ShouldUpdateEmailOnly()
     {
         var newUser = new CreateUserDto { Name = "User", Email = "old@email.com", Password = "password123", Role = UserRole.Admin };
         var createResponse = await _client.PostAsync("/api/users", JsonContent.Create(newUser, options: CustomWebApplicationFactory.JsonOptions));
         var createdUser = await createResponse.Content.ReadFromJsonAsync<UserDto>(CustomWebApplicationFactory.JsonOptions);

         var updateDto = new UpdateUserDto { Email = "new@email.com" };
         var response = await _client.PatchAsync($"/api/users/{createdUser?.UserId}", JsonContent.Create(updateDto, options: CustomWebApplicationFactory.JsonOptions));

         response.EnsureSuccessStatusCode();
         var updatedUser = await response.Content.ReadFromJsonAsync<UserDto>(CustomWebApplicationFactory.JsonOptions);
         Assert.Equal("User", updatedUser?.Name);
         Assert.Equal("new@email.com", updatedUser?.Email);
     }

     [Fact]
     public async Task UpdateUser_WhenUpdatingOnlyRole_ShouldUpdateRoleOnly()
     {
         var newUser = new CreateUserDto { Name = "User", Email = "user@email.com", Password = "password123", Role = UserRole.Admin };
         var createResponse = await _client.PostAsync("/api/users", JsonContent.Create(newUser, options: CustomWebApplicationFactory.JsonOptions));
         var createdUser = await createResponse.Content.ReadFromJsonAsync<UserDto>(CustomWebApplicationFactory.JsonOptions);

         var updateDto = new UpdateUserDto { Role = UserRole.Customer };
         var response = await _client.PatchAsync($"/api/users/{createdUser?.UserId}", JsonContent.Create(updateDto, options: CustomWebApplicationFactory.JsonOptions));

         response.EnsureSuccessStatusCode();
         var updatedUser = await response.Content.ReadFromJsonAsync<UserDto>(CustomWebApplicationFactory.JsonOptions);
         Assert.Equal("User", updatedUser?.Name);
         Assert.Equal("user@email.com", updatedUser?.Email);
         Assert.Equal(UserRole.Customer, updatedUser?.Role);
     }
}
