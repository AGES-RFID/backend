using System.Net;
using System.Net.Http.Json;
using Backend.Database;
using Backend.Features.Accesses;
using Backend.Features.Tags;
using Backend.Features.Tags.Enums;
using Backend.Features.Transactions;
using Backend.Features.Users;
using Backend.Features.Vehicles;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using tests.Setup;

namespace Backend.Tests.Integration.Features.Accesses;

public class AccessesControllerTests(CustomWebApplicationFactory factory) : IClassFixture<CustomWebApplicationFactory>, IAsyncLifetime
{
    private readonly HttpClient _client = factory.CreateClient();
    private readonly IServiceScopeFactory _scopeFactory = factory.Services.GetRequiredService<IServiceScopeFactory>();

    public async Task InitializeAsync()
    {
        await factory.ResetDatabaseAsync();
    }

    public Task DisposeAsync() => Task.CompletedTask;

    private async Task<(Guid TagId, string Tid, string Epc)> SeedVehicleAndTagAsync(TagStatus status = TagStatus.IN_USE, string? plate = null)
    {
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var user = new User { Name = "Test", Email = $"test_{Guid.NewGuid()}@example.com", PasswordHash = "hash", Role = UserRole.Customer };
        db.Users.Add(user);

        var tid = $"TID-{Guid.NewGuid()}";
        var epc = $"EPC-{Guid.NewGuid()}";
        var tag = new Tag { Status = status, Epc = epc, Tid = tid };
        db.Tags.Add(tag);

        db.Vehicles.Add(new Vehicle
        {
            UserId = user.UserId,
            TagId = tag.TagId,
            Plate = plate ?? $"TST{Guid.NewGuid().ToString()[..4].ToUpper()}",
            Brand = "VW",
            Model = "Gol"
        });

        await db.SaveChangesAsync();
        return (tag.TagId, tid, epc);
    }

    private async Task<Guid> SeedAccessAsync(Guid tagId, AccessType type, DateTime? timestamp = null)
    {
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var tag = await db.Tags.FirstAsync(t => t.TagId == tagId);

        var access = new Access
        {
            TagId = tagId,
            Tag = tag,
            Type = type,
            Timestamp = timestamp ?? DateTime.UtcNow.AddMinutes(-5)
        };

        db.Accesses.Add(access);

        await db.SaveChangesAsync();

        return access.AccessId;
    }

    private async Task SeedTransactionAsync(Guid accessId, decimal amount, DateTime createdAt)
    {
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var user = await db.Users.FirstAsync();

        db.Transactions.Add(new Transaction
        {
            UserId = user.UserId,
            AccessId = accessId,
            Amount = amount,
            Description = "Saída",
            TransactionType = TransactionType.WITHDRAWAL,
            CreatedAt = createdAt,
        });

        await db.SaveChangesAsync();
    }

    [Fact]
    public async Task PostAccess_Entry_WithValidTag_ReturnsCreated()
    {
        var (_, tid, epc) = await SeedVehicleAndTagAsync();
        var payload = new CreateAccessDto { Tid = tid, Epc = epc, Entrance = true };

        var response = await _client.PostAsync("/api/accesses", JsonContent.Create(payload, options: CustomWebApplicationFactory.JsonOptions));

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<AccessDto>(CustomWebApplicationFactory.JsonOptions);
        Assert.NotNull(result);
        Assert.Equal(AccessType.Entry, result.Type);
    }

    [Fact]
    public async Task PostAccess_Entry_WhenAlreadyInside_ReturnsConflict()
    {
        var (_, tid, epc) = await SeedVehicleAndTagAsync();
        var payload = new CreateAccessDto { Tid = tid, Epc = epc, Entrance = true };

        await _client.PostAsync("/api/accesses", JsonContent.Create(payload, options: CustomWebApplicationFactory.JsonOptions));

        var response = await _client.PostAsync("/api/accesses", JsonContent.Create(payload, options: CustomWebApplicationFactory.JsonOptions));

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task PostAccess_Exit_WhenSuccessfullyEntered_ReturnsCreated()
    {
        var (_, tid, epc) = await SeedVehicleAndTagAsync();
        var entryPayload = new CreateAccessDto { Tid = tid, Epc = epc, Entrance = true };
        var exitPayload = new CreateAccessDto { Tid = tid, Epc = epc, Entrance = false };

        await _client.PostAsync("/api/accesses", JsonContent.Create(entryPayload, options: CustomWebApplicationFactory.JsonOptions));

        var response = await _client.PostAsync("/api/accesses", JsonContent.Create(exitPayload, options: CustomWebApplicationFactory.JsonOptions));

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<AccessDto>(CustomWebApplicationFactory.JsonOptions);
        Assert.Equal(AccessType.Exit, result?.Type);
    }

    [Fact]
    public async Task PostAccess_Exit_WithoutPriorEntry_ReturnsConflict()
    {
        var (_, tid, epc) = await SeedVehicleAndTagAsync();
        var payload = new CreateAccessDto { Tid = tid, Epc = epc, Entrance = false };

        var response = await _client.PostAsync("/api/accesses", JsonContent.Create(payload, options: CustomWebApplicationFactory.JsonOptions));

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task PostAccess_WithNonExistentTag_ReturnsNotFound()
    {
        var payload = new CreateAccessDto { Tid = "NONEXISTENT-TID", Epc = "NONEXISTENT-EPC", Entrance = true };

        var response = await _client.PostAsync("/api/accesses", JsonContent.Create(payload, options: CustomWebApplicationFactory.JsonOptions));

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task PostAccess_Entry_WithInactiveTag_ReturnsConflict()
    {
        var (_, tid, epc) = await SeedVehicleAndTagAsync(TagStatus.INACTIVE);
        var payload = new CreateAccessDto { Tid = tid, Epc = epc, Entrance = true };

        var response = await _client.PostAsync("/api/accesses", JsonContent.Create(payload, options: CustomWebApplicationFactory.JsonOptions));

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task PostAccess_Exit_WithInactiveTag_ReturnsConflict()
    {
        var (tagId, tid, epc) = await SeedVehicleAndTagAsync(TagStatus.INACTIVE);
        await SeedAccessAsync(tagId, AccessType.Entry);
        var payload = new CreateAccessDto { Tid = tid, Epc = epc, Entrance = false };

        var response = await _client.PostAsync("/api/accesses", JsonContent.Create(payload, options: CustomWebApplicationFactory.JsonOptions));

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task GetAccesses_WhenNotAuthenticated_ReturnsUnauthorized()
    {
        var anonymousClient = AuthTestHelper.CreateAnonymousClient(factory);

        var response = await anonymousClient.GetAsync("/api/accesses");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetAccesses_WhenCustomerAuthenticated_ReturnsForbidden()
    {
        var customerClient = await AuthTestHelper.CreateClientAsAsync(factory, UserRole.Customer);

        var response = await customerClient.GetAsync("/api/accesses");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task GetAccesses_WhenAdminAndEmpty_ReturnsOkWithEmptyList()
    {
        var adminClient = await AuthTestHelper.CreateClientAsAsync(factory, UserRole.Admin);

        var response = await adminClient.GetAsync("/api/accesses");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<List<AccessDto>>(CustomWebApplicationFactory.JsonOptions);
        Assert.NotNull(result);
        Assert.Empty(result);
    }

    [Fact]
    public async Task GetAccesses_WithAccessTypeExit_ReturnsOnlyExitsWithPlateAndNullableValue()
    {
        var adminClient = await AuthTestHelper.CreateClientAsAsync(factory, UserRole.Admin);

        var (entryTagId, _, _) = await SeedVehicleAndTagAsync();
        var (exitTagIdWithCharge, _, _) = await SeedVehicleAndTagAsync();
        var (exitTagIdWithoutCharge, _, _) = await SeedVehicleAndTagAsync();

        await SeedAccessAsync(entryTagId, AccessType.Entry);

        var chargedExitId = await SeedAccessAsync(exitTagIdWithCharge, AccessType.Exit);
        await SeedTransactionAsync(chargedExitId, 17.5m, DateTime.UtcNow.AddMinutes(-1));

        await SeedAccessAsync(exitTagIdWithoutCharge, AccessType.Exit);

        var response = await adminClient.GetAsync("/api/accesses?accessType=exit");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var result = await response.Content.ReadFromJsonAsync<List<AccessDto>>(CustomWebApplicationFactory.JsonOptions);
        Assert.NotNull(result);
        Assert.Equal(2, result.Count);
        Assert.All(result, row => Assert.Equal(AccessType.Exit, row.Type));
        Assert.All(result, row => Assert.False(string.IsNullOrWhiteSpace(row.Plate)));
        Assert.Contains(result, row => row.Value is null);
        Assert.Contains(result, row => row.Value == 17.5m);
    }

    [Fact]
    public async Task GetAccesses_WhenMultipleTransactionsForSameAccess_UsesLatestTransactionValue()
    {
        var adminClient = await AuthTestHelper.CreateClientAsAsync(factory, UserRole.Admin);

        var (tagId, _, _) = await SeedVehicleAndTagAsync();
        var accessId = await SeedAccessAsync(tagId, AccessType.Exit);

        await SeedTransactionAsync(accessId, 10m, DateTime.UtcNow.AddMinutes(-10));
        await SeedTransactionAsync(accessId, 25m, DateTime.UtcNow.AddMinutes(-1));

        var response = await adminClient.GetAsync("/api/accesses?accessType=exit");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var result = await response.Content.ReadFromJsonAsync<List<AccessDto>>(CustomWebApplicationFactory.JsonOptions);
        Assert.NotNull(result);

        var row = Assert.Single(result);
        Assert.Equal(25m, row.Value);
    }

    [Fact]
    public async Task GetTimeseries_WhenAdmin_ReturnsTimeseriesData()
    {
        var adminClient = await AuthTestHelper.CreateClientAsAsync(factory, UserRole.Admin);

        var (tagId, _, _) = await SeedVehicleAndTagAsync();
        
        await SeedAccessAsync(tagId, AccessType.Entry, DateTime.UtcNow.AddHours(-2));
        await SeedAccessAsync(tagId, AccessType.Exit, DateTime.UtcNow.AddHours(-1));

        var response = await adminClient.GetAsync("/api/accesses/timeseries");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var result = await response.Content.ReadFromJsonAsync<TimeseriesResponseDto>(CustomWebApplicationFactory.JsonOptions);
        Assert.NotNull(result);
        Assert.Equal(2, result.Series.Count());
        
        var entries = result.Series.First(s => s.Key == "entries");
        var exits = result.Series.First(s => s.Key == "exits");

        Assert.Equal(24, entries.Points.Count());
        Assert.Equal(24, exits.Points.Count());

        var entryCount = entries.Points.Sum(p => p.Count);
        var exitCount = exits.Points.Sum(p => p.Count);
        
        Assert.True(entryCount >= 1, "Should have at least 1 entry");
        Assert.True(exitCount >= 1, "Should have at least 1 exit");
    }
}
