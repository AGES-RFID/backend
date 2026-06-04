using System.Net;
using System.Net.Http.Json;
using Backend.Database;
using Backend.Features.Transactions;
using Backend.Features.Users;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using tests.Setup;

namespace tests.Features.Users;

public class UserControllerTests(CustomWebApplicationFactory factory) : IClassFixture<CustomWebApplicationFactory>, IAsyncLifetime
{
    private readonly CustomWebApplicationFactory _factory = factory;
    private readonly HttpClient _client = factory.CreateClient();

    public async Task InitializeAsync()
    {
        await factory.ResetDatabaseAsync();
    }

    public Task DisposeAsync() => Task.CompletedTask;

    public static IEnumerable<object[]> AdminProtectedEndpoints()
    {
        yield return ["GET", "/api/users"];
        yield return ["GET", $"/api/users/{Guid.Parse("11111111-1111-1111-1111-111111111111")}"];
        yield return ["GET", "/api/users/by-name/any"];
        yield return ["DELETE", $"/api/users/{Guid.Parse("22222222-2222-2222-2222-222222222222")}"];
    }

    [Theory]
    [MemberData(nameof(AdminProtectedEndpoints))]
    public async Task AdminProtectedEndpoints_WhenAnonymous_ReturnUnauthorized(string method, string path)
    {
        var client = AuthTestHelper.CreateAnonymousClient(_factory);

        var response = method switch
        {
            "GET" => await client.GetAsync(path),
            "DELETE" => await client.DeleteAsync(path),
            _ => throw new InvalidOperationException($"Unsupported method '{method}'")
        };

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Theory]
    [MemberData(nameof(AdminProtectedEndpoints))]
    public async Task AdminProtectedEndpoints_WhenCustomer_ReturnForbidden(string method, string path)
    {
        var customerClient = await AuthTestHelper.CreateClientAsAsync(_factory, UserRole.Customer);

        var response = method switch
        {
            "GET" => await customerClient.GetAsync(path),
            "DELETE" => await customerClient.DeleteAsync(path),
            _ => throw new InvalidOperationException($"Unsupported method '{method}'")
        };

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task CreateUser_WhenAnonymous_AlwaysCreatesCustomer()
    {
        var anonymousClient = AuthTestHelper.CreateAnonymousClient(_factory);
        var newUser = new CreateUserDto
        {
            Name = "Signup User",
            Email = "signup@email.com",
            Password = "password123",
            Role = UserRole.Admin
        };

        var response = await anonymousClient.PostAsync("/api/users", JsonContent.Create(newUser, options: CustomWebApplicationFactory.JsonOptions));

        response.EnsureSuccessStatusCode();
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var createdUser = await response.Content.ReadFromJsonAsync<UserDto>(CustomWebApplicationFactory.JsonOptions);
        Assert.NotNull(createdUser);
        Assert.Equal(UserRole.Customer, createdUser.Role);
    }

    [Fact]
    public async Task CreateUser_WhenCustomerAuthenticated_ReturnsForbidden()
    {
        var customerClient = await AuthTestHelper.CreateClientAsAsync(_factory, UserRole.Customer);
        var dto = new CreateUserDto
        {
            Name = "Blocked",
            Email = "blocked@email.com",
            Password = "password123",
            Role = UserRole.Customer
        };

        var response = await customerClient.PostAsync("/api/users", JsonContent.Create(dto, options: CustomWebApplicationFactory.JsonOptions));

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task CreateUser_WhenAdminAuthenticated_CanCreateAdmin()
    {
        var adminClient = await AuthTestHelper.CreateClientAsAsync(_factory, UserRole.Admin);
        var dto = new CreateUserDto
        {
            Name = "Admin Created",
            Email = "new-admin@email.com",
            Password = "password123",
            Role = UserRole.Admin
        };

        var response = await adminClient.PostAsync("/api/users", JsonContent.Create(dto, options: CustomWebApplicationFactory.JsonOptions));

        response.EnsureSuccessStatusCode();

        var createdUser = await response.Content.ReadFromJsonAsync<UserDto>(CustomWebApplicationFactory.JsonOptions);
        Assert.NotNull(createdUser);
        Assert.Equal(UserRole.Admin, createdUser.Role);
    }

    [Fact]
    public async Task GetUsers_WhenAdmin_ReturnsSuccess()
    {
        var adminClient = await AuthTestHelper.CreateClientAsAsync(_factory, UserRole.Admin);

        var response = await adminClient.GetAsync("/api/users");

        response.EnsureSuccessStatusCode();
        var users = await response.Content.ReadFromJsonAsync<List<UserDto>>(CustomWebApplicationFactory.JsonOptions);
        Assert.NotNull(users);
    }

    [Fact]
    public async Task GetUser_WhenAdminAndNotFound_ReturnsNotFound()
    {
        var adminClient = await AuthTestHelper.CreateClientAsAsync(_factory, UserRole.Admin);

        var response = await adminClient.GetAsync($"/api/users/{Guid.NewGuid()}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task GetUser_WhenAdmin_ReturnsBalanceFromTransactions()
    {
        var adminClient = await AuthTestHelper.CreateClientAsAsync(_factory, UserRole.Admin);
        var newUser = new CreateUserDto { Name = "Balance User", Email = "balance@email.com", Password = "password123", Role = UserRole.Customer };

        var createResponse = await adminClient.PostAsync("/api/users", JsonContent.Create(newUser, options: CustomWebApplicationFactory.JsonOptions));
        createResponse.EnsureSuccessStatusCode();
        var createdUser = await createResponse.Content.ReadFromJsonAsync<UserDto>(CustomWebApplicationFactory.JsonOptions);

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            db.Transactions.AddRange(
                new Transaction
                {
                    UserId = createdUser!.UserId,
                    Amount = 100m,
                    Description = "Initial deposit",
                    TransactionType = TransactionType.DEPOSIT
                },
                new Transaction
                {
                    UserId = createdUser.UserId,
                    Amount = 30m,
                    Description = "Withdrawal",
                    TransactionType = TransactionType.WITHDRAWAL
                });
            await db.SaveChangesAsync();
        }

        var getResponse = await adminClient.GetAsync($"/api/users/{createdUser?.UserId}");
        getResponse.EnsureSuccessStatusCode();
        var fetchedUser = await getResponse.Content.ReadFromJsonAsync<UserWithVehiclesDto>(CustomWebApplicationFactory.JsonOptions);

        Assert.NotNull(fetchedUser);
        Assert.Equal(70m, fetchedUser.Balance);
    }

    [Fact]
    public async Task UpdateUser_WhenAnonymous_ReturnsUnauthorized()
    {
        var anonymousClient = AuthTestHelper.CreateAnonymousClient(_factory);
        var response = await anonymousClient.PatchAsync($"/api/users/{Guid.NewGuid()}", JsonContent.Create(new UpdateUserDto { Name = "No Auth" }, options: CustomWebApplicationFactory.JsonOptions));

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task UpdateUser_WhenCustomerUpdatesAnotherUser_ReturnsForbidden()
    {
        var anonymousClient = AuthTestHelper.CreateAnonymousClient(_factory);
        var createResponse = await anonymousClient.PostAsync("/api/users", JsonContent.Create(new CreateUserDto
        {
            Name = "Target",
            Email = "target@email.com",
            Password = "password123",
            Role = UserRole.Customer
        }, options: CustomWebApplicationFactory.JsonOptions));

        var targetUser = await createResponse.Content.ReadFromJsonAsync<UserDto>(CustomWebApplicationFactory.JsonOptions);
        var customerClient = await AuthTestHelper.CreateClientAsAsync(_factory, UserRole.Customer);

        var response = await customerClient.PatchAsync($"/api/users/{targetUser!.UserId}", JsonContent.Create(new UpdateUserDto { Name = "Hack" }, options: CustomWebApplicationFactory.JsonOptions));

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task UpdateUser_WhenCustomerUpdatesSelf_CanUpdateAndCannotEscalateRole()
    {
        var customerEmail = $"self_{Guid.NewGuid()}@email.com";
        var customerClient = await AuthTestHelper.CreateClientAsAsync(_factory, UserRole.Customer, email: customerEmail);
        var userId = await GetUserIdByEmailAsync(customerEmail);

        var response = await customerClient.PatchAsync(
            $"/api/users/{userId}",
            JsonContent.Create(new UpdateUserDto
            {
                Name = "Updated Self",
                Role = UserRole.Admin
            }, options: CustomWebApplicationFactory.JsonOptions)
        );

        response.EnsureSuccessStatusCode();

        var updated = await response.Content.ReadFromJsonAsync<UserDto>(CustomWebApplicationFactory.JsonOptions);
        Assert.NotNull(updated);
        Assert.Equal("Updated Self", updated.Name);
        Assert.Equal(UserRole.Customer, updated.Role);
    }

    [Fact]
    public async Task UpdateUser_WhenAdminUpdatingRole_ShouldUpdateRole()
    {
        var adminClient = await AuthTestHelper.CreateClientAsAsync(_factory, UserRole.Admin);
        var createResponse = await adminClient.PostAsync("/api/users", JsonContent.Create(new CreateUserDto
        {
            Name = "User",
            Email = "user@email.com",
            Password = "password123",
            Role = UserRole.Customer
        }, options: CustomWebApplicationFactory.JsonOptions));

        var createdUser = await createResponse.Content.ReadFromJsonAsync<UserDto>(CustomWebApplicationFactory.JsonOptions);

        var response = await adminClient.PatchAsync(
            $"/api/users/{createdUser!.UserId}",
            JsonContent.Create(new UpdateUserDto { Role = UserRole.Admin }, options: CustomWebApplicationFactory.JsonOptions)
        );

        response.EnsureSuccessStatusCode();

        var updatedUser = await response.Content.ReadFromJsonAsync<UserDto>(CustomWebApplicationFactory.JsonOptions);
        Assert.NotNull(updatedUser);
        Assert.Equal(UserRole.Admin, updatedUser.Role);
    }

    [Fact]
    public async Task CreateUser_WhenEmailAlreadyExists_ShouldReturnConflict()
    {
        var anonymousClient = AuthTestHelper.CreateAnonymousClient(_factory);
        var newUser = new CreateUserDto { Name = "Fulaninho", Email = "fulano@email.com", Password = "password123", Role = UserRole.Customer };
        await anonymousClient.PostAsync("/api/users", JsonContent.Create(newUser, options: CustomWebApplicationFactory.JsonOptions));

        var response = await anonymousClient.PostAsync("/api/users", JsonContent.Create(newUser, options: CustomWebApplicationFactory.JsonOptions));
        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    private async Task<Guid> GetUserIdByEmailAsync(string email)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var user = await db.Users.FirstAsync(u => u.Email == email);
        return user.UserId;
    }
}
