using System.Net;
using System.Net.Http.Json;
using Backend.Features.Auth;
using Backend.Features.Users;
using tests.Setup;

namespace Backend.Tests.Integration.Features.Auth;

public class AuthControllerTests(CustomWebApplicationFactory factory) : IClassFixture<CustomWebApplicationFactory>, IAsyncLifetime
{
    private readonly CustomWebApplicationFactory _factory = factory;
    private readonly HttpClient _client = factory.CreateClient();

    public async Task InitializeAsync()
    {
        await factory.ResetDatabaseAsync();
    }

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task Login_WithValidCredentials_ReturnsOkWithAuthResponse()
    {
        var newUser = new CreateUserDto { Name = "Test User", Email = "test@example.com", Password = "password123", Role = UserRole.Admin };
        var createResponse = await _client.PostAsync("/api/users", JsonContent.Create(newUser, options: CustomWebApplicationFactory.JsonOptions));
        createResponse.EnsureSuccessStatusCode();

        var loginDto = new LoginDto { Email = "test@example.com", Password = "password123" };
        var loginResponse = await _client.PostAsync("/api/auth/login", JsonContent.Create(loginDto, options: CustomWebApplicationFactory.JsonOptions));

        loginResponse.EnsureSuccessStatusCode();
        Assert.Equal(HttpStatusCode.OK, loginResponse.StatusCode);

        var authResponse = await loginResponse.Content.ReadFromJsonAsync<AuthResponse>(CustomWebApplicationFactory.JsonOptions);
        Assert.NotNull(authResponse);
        Assert.NotEmpty(authResponse.Token);
        Assert.Equal("test@example.com", authResponse.User.Email);
    }

    [Fact]
    public async Task Login_WithInvalidEmail_ReturnsUnauthorized()
    {
        var loginDto = new LoginDto { Email = "nonexistent@example.com", Password = "password123" };
        var loginResponse = await _client.PostAsync("/api/auth/login", JsonContent.Create(loginDto, options: CustomWebApplicationFactory.JsonOptions));

        Assert.Equal(HttpStatusCode.Unauthorized, loginResponse.StatusCode);
    }

    [Fact]
    public async Task Login_WithInvalidPassword_ReturnsUnauthorized()
    {
        var newUser = new CreateUserDto { Name = "Test User", Email = "test@example.com", Password = "correct123", Role = UserRole.Admin };
        var createResponse = await _client.PostAsync("/api/users", JsonContent.Create(newUser, options: CustomWebApplicationFactory.JsonOptions));
        createResponse.EnsureSuccessStatusCode();

        var loginDto = new LoginDto { Email = "test@example.com", Password = "wrongpassword" };
        var loginResponse = await _client.PostAsync("/api/auth/login", JsonContent.Create(loginDto, options: CustomWebApplicationFactory.JsonOptions));

        Assert.Equal(HttpStatusCode.Unauthorized, loginResponse.StatusCode);
    }

    [Fact]
    public async Task Login_MultipleUsers_CanLoginWithDifferentCredentials()
    {
        var user1Dto = new CreateUserDto { Name = "User One", Email = "user1@example.com", Password = "password1", Role = UserRole.Admin };
        var user2Dto = new CreateUserDto { Name = "User Two", Email = "user2@example.com", Password = "password2", Role = UserRole.Customer };

        await _client.PostAsync("/api/users", JsonContent.Create(user1Dto, options: CustomWebApplicationFactory.JsonOptions));
        await _client.PostAsync("/api/users", JsonContent.Create(user2Dto, options: CustomWebApplicationFactory.JsonOptions));

        var login1 = new LoginDto { Email = "user1@example.com", Password = "password1" };
        var response1 = await _client.PostAsync("/api/auth/login", JsonContent.Create(login1, options: CustomWebApplicationFactory.JsonOptions));
        response1.EnsureSuccessStatusCode();
        var auth1 = await response1.Content.ReadFromJsonAsync<AuthResponse>(CustomWebApplicationFactory.JsonOptions);

        var login2 = new LoginDto { Email = "user2@example.com", Password = "password2" };
        var response2 = await _client.PostAsync("/api/auth/login", JsonContent.Create(login2, options: CustomWebApplicationFactory.JsonOptions));
        response2.EnsureSuccessStatusCode();
        var auth2 = await response2.Content.ReadFromJsonAsync<AuthResponse>(CustomWebApplicationFactory.JsonOptions);

        Assert.NotEqual(auth1?.Token, auth2?.Token);
        Assert.Equal("user1@example.com", auth1?.User.Email);
        Assert.Equal("user2@example.com", auth2?.User.Email);
    }

    [Fact]
    public async Task GetCurrentUser_WithValidToken_ReturnsUserWithVehicles()
    {
        var newUser = new CreateUserDto { Name = "Test User", Email = "current@example.com", Password = "password123", Role = UserRole.Admin };
        var createResponse = await _client.PostAsync("/api/users", JsonContent.Create(newUser, options: CustomWebApplicationFactory.JsonOptions));
        createResponse.EnsureSuccessStatusCode();

        var loginDto = new LoginDto { Email = "current@example.com", Password = "password123" };
        var loginResponse = await _client.PostAsync("/api/auth/login", JsonContent.Create(loginDto, options: CustomWebApplicationFactory.JsonOptions));
        loginResponse.EnsureSuccessStatusCode();
        var authResponse = await loginResponse.Content.ReadFromJsonAsync<AuthResponse>(CustomWebApplicationFactory.JsonOptions);

        var authenticatedClient = _factory.CreateClient();
        authenticatedClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", authResponse?.Token);

        var meResponse = await authenticatedClient.GetAsync("/api/auth/me");
        meResponse.EnsureSuccessStatusCode();

        var user = await meResponse.Content.ReadFromJsonAsync<UserWithVehiclesDto>(CustomWebApplicationFactory.JsonOptions);
        Assert.NotNull(user);
        Assert.Equal("current@example.com", user.Email);
        Assert.Equal("Test User", user.Name);
        Assert.NotNull(user.Vehicles);
    }

    [Fact]
    public async Task GetCurrentUser_WithoutToken_ReturnsUnauthorized()
    {
        var response = await _client.GetAsync("/api/auth/me");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetCurrentUser_WithInvalidToken_ReturnsUnauthorized()
    {
        var authenticatedClient = _factory.CreateClient();
        authenticatedClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", "invalid.token.here");

        var response = await authenticatedClient.GetAsync("/api/auth/me");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }
}
