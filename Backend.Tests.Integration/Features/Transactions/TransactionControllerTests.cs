using System.Net;
using System.Net.Http.Json;
using Backend.Database;
using Backend.Features.Transactions;
using Backend.Features.Users;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using tests.Setup;

namespace tests.Features.Transactions;

public class TransactionControllerTests(CustomWebApplicationFactory factory) : IClassFixture<CustomWebApplicationFactory>, IAsyncLifetime
{
    private readonly IServiceScopeFactory _scopeFactory = factory.Services.GetRequiredService<IServiceScopeFactory>();

    public async Task InitializeAsync()
    {
        await factory.ResetDatabaseAsync();
    }

    public Task DisposeAsync() => Task.CompletedTask;

    private async Task<User> SeedUserAsync(UserRole role)
    {
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var user = new User
        {
            Name = $"User-{Guid.NewGuid()}",
            Email = $"user_{Guid.NewGuid()}@example.com",
            PasswordHash = "hash",
            Role = role
        };

        db.Users.Add(user);
        await db.SaveChangesAsync();
        return user;
    }

    private async Task<User> GetUserByEmailAsync(string email)
    {
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        return await db.Users.FirstAsync(u => u.Email == email);
    }

    [Fact]
    public async Task CreateTransaction_AdminCanCreateForOtherUser_ReturnsCreated()
    {
        var adminClient = await AuthTestHelper.CreateClientAsAsync(factory, UserRole.Admin);
        var customer = await SeedUserAsync(UserRole.Customer);

        var payload = new CreateTransactionDto
        {
            UserId = customer.UserId,
            Description = "Admin deposit",
            Amount = 10m
        };

        var response = await adminClient.PostAsync("/api/transactions", JsonContent.Create(payload, options: CustomWebApplicationFactory.JsonOptions));

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var created = await response.Content.ReadFromJsonAsync<TransactionDto>(CustomWebApplicationFactory.JsonOptions);
        Assert.NotNull(created);
        Assert.Equal(customer.UserId, created.UserId);
        Assert.Equal(payload.Amount, created.Amount);
    }

    [Fact]
    public async Task CreateTransaction_CustomerCanCreateForSelf_WhenUserIdOmitted()
    {
        const string customerEmail = "customer.self@example.com";
        var customerClient = await AuthTestHelper.CreateClientAsAsync(factory, UserRole.Customer, email: customerEmail);
        var customer = await GetUserByEmailAsync(customerEmail);

        var payload = new CreateTransactionDto
        {
            Description = "Self deposit",
            Amount = 25m
        };

        var response = await customerClient.PostAsync("/api/transactions", JsonContent.Create(payload, options: CustomWebApplicationFactory.JsonOptions));

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var created = await response.Content.ReadFromJsonAsync<TransactionDto>(CustomWebApplicationFactory.JsonOptions);
        Assert.NotNull(created);
        Assert.Equal(customer.UserId, created.UserId);
    }

    [Fact]
    public async Task CreateTransaction_CustomerCannotCreateForOtherUser_ReturnsForbidden()
    {
        var customerClient = await AuthTestHelper.CreateClientAsAsync(factory, UserRole.Customer);
        var otherCustomer = await SeedUserAsync(UserRole.Customer);

        var payload = new CreateTransactionDto
        {
            UserId = otherCustomer.UserId,
            Description = "Invalid deposit",
            Amount = 10m
        };

        var response = await customerClient.PostAsync("/api/transactions", JsonContent.Create(payload, options: CustomWebApplicationFactory.JsonOptions));

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task CreateTransaction_AdminOmittingUserId_DefaultsToSelf()
    {
        const string adminEmail = "admin.self@example.com";
        var adminClient = await AuthTestHelper.CreateClientAsAsync(factory, UserRole.Admin, email: adminEmail);
        var admin = await GetUserByEmailAsync(adminEmail);

        var payload = new CreateTransactionDto
        {
            Description = "Admin self deposit",
            Amount = 50m
        };

        var response = await adminClient.PostAsync("/api/transactions", JsonContent.Create(payload, options: CustomWebApplicationFactory.JsonOptions));

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var created = await response.Content.ReadFromJsonAsync<TransactionDto>(CustomWebApplicationFactory.JsonOptions);
        Assert.NotNull(created);
        Assert.Equal(admin.UserId, created.UserId);
    }

    [Fact]
    public async Task CreateTransaction_WhenUnauthorized_ReturnsUnauthorized()
    {
        var anonymousClient = AuthTestHelper.CreateAnonymousClient(factory);

        var payload = new CreateTransactionDto
        {
            Description = "No auth",
            Amount = 15m
        };

        var response = await anonymousClient.PostAsync("/api/transactions", JsonContent.Create(payload, options: CustomWebApplicationFactory.JsonOptions));

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task CreateTransaction_WhenTargetUserDoesNotExist_ReturnsNotFound()
    {
        var adminClient = await AuthTestHelper.CreateClientAsAsync(factory, UserRole.Admin);

        var payload = new CreateTransactionDto
        {
            UserId = Guid.NewGuid(),
            Description = "Missing user",
            Amount = 10m
        };

        var response = await adminClient.PostAsync("/api/transactions", JsonContent.Create(payload, options: CustomWebApplicationFactory.JsonOptions));

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task CreateTransaction_WhenPayloadInvalid_ReturnsBadRequest()
    {
        var adminClient = await AuthTestHelper.CreateClientAsAsync(factory, UserRole.Admin);

        var payload = new CreateTransactionDto
        {
            Description = "",
            Amount = 0m
        };

        var response = await adminClient.PostAsync("/api/transactions", JsonContent.Create(payload, options: CustomWebApplicationFactory.JsonOptions));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task GetMyTransactions_WhenAuthenticated_ReturnsOk()
    {
        var customerClient = await AuthTestHelper.CreateClientAsAsync(factory, UserRole.Customer);

        var response = await customerClient.GetAsync("/api/transactions");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task GetMyTransactions_WhenNotAuthenticated_ReturnsUnauthorized()
    {
        var anonymousClient = AuthTestHelper.CreateAnonymousClient(factory);

        var response = await anonymousClient.GetAsync("/api/transactions");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetMyTransactions_WhenNoTransactions_ReturnsEmptyList()
    {
        var customerClient = await AuthTestHelper.CreateClientAsAsync(factory, UserRole.Customer);

        var response = await customerClient.GetAsync("/api/transactions");
        var transactions = await response.Content.ReadFromJsonAsync<List<TransactionDto>>(CustomWebApplicationFactory.JsonOptions);

        Assert.NotNull(transactions);
        Assert.Empty(transactions);
    }

    [Fact]
    public async Task GetMyTransactions_AfterCreating_ReturnsOwnTransactions()
    {
        var customerClient = await AuthTestHelper.CreateClientAsAsync(factory, UserRole.Customer);

        var dto = new CreateTransactionDto { Description = "Depósito", Amount = 100 };
        await customerClient.PostAsync("/api/transactions", JsonContent.Create(dto, options: CustomWebApplicationFactory.JsonOptions));

        var response = await customerClient.GetAsync("/api/transactions");
        var transactions = await response.Content.ReadFromJsonAsync<List<TransactionDto>>(CustomWebApplicationFactory.JsonOptions);

        Assert.NotNull(transactions);
        Assert.Single(transactions);
        Assert.Equal(100, transactions[0].Amount);
    }

    [Fact]
    public async Task GetMyTransactions_ReturnsOnlyOwnTransactions()
    {
        var customer1Client = await AuthTestHelper.CreateClientAsAsync(factory, UserRole.Customer);
        var customer2Client = await AuthTestHelper.CreateClientAsAsync(factory, UserRole.Customer);

        var dto = new CreateTransactionDto { Description = "Depósito", Amount = 50 };
        await customer1Client.PostAsync("/api/transactions", JsonContent.Create(dto, options: CustomWebApplicationFactory.JsonOptions));

        var response = await customer2Client.GetAsync("/api/transactions");
        var transactions = await response.Content.ReadFromJsonAsync<List<TransactionDto>>(CustomWebApplicationFactory.JsonOptions);

        Assert.Empty(transactions!);
    }
}
