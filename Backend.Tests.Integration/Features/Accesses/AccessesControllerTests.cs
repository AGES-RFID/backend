using System.Net;
using System.Net.Http.Json;
using Backend.Database;
using Backend.Features.Accesses;
using Backend.Features.Tags;
using Backend.Features.Tags.Enums;
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

    private async Task<(Guid TagId, string Tid, string Epc)> SeedVehicleAndTagAsync(TagStatus status = TagStatus.IN_USE)
    {
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var user = new User { Name = "Test", Email = $"test_{Guid.NewGuid()}@example.com", PasswordHash = "hash", Role = UserRole.Customer };
        db.Users.Add(user);

        var tid = $"TID-{Guid.NewGuid()}";
        var epc = $"EPC-{Guid.NewGuid()}";
        var tag = new Tag { Status = status, Epc = epc, Tid = tid };
        db.Tags.Add(tag);

        db.Vehicles.Add(new Vehicle { UserId = user.UserId, TagId = tag.TagId, Plate = $"TST{Guid.NewGuid().ToString()[..4].ToUpper()}", Brand = "VW", Model = "Gol" });

        await db.SaveChangesAsync();
        return (tag.TagId, tid, epc);
    }

    private async Task SeedAccessAsync(Guid tagId, AccessType type)
    {
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var tag = await db.Tags.FirstAsync(t => t.TagId == tagId);

        db.Accesses.Add(new Access
        {
            TagId = tagId,
            Tag = tag,
            Type = type,
            Timestamp = DateTime.UtcNow.AddMinutes(-5)
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
    public async Task GetAccesses_WhenEmpty_ReturnsOkWithEmptyList()
    {
        var response = await _client.GetAsync("/api/accesses");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<List<AccessDto>>(CustomWebApplicationFactory.JsonOptions);
        Assert.NotNull(result);
        Assert.Empty(result);
    }

    [Fact]
    public async Task GetAccesses_WithSeededData_ReturnsAllAccesses()
    {
        var (tagId, _, _) = await SeedVehicleAndTagAsync();
        await SeedAccessAsync(tagId, AccessType.Entry);
        await SeedAccessAsync(tagId, AccessType.Exit);

        var response = await _client.GetAsync("/api/accesses");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<List<AccessDto>>(CustomWebApplicationFactory.JsonOptions);
        Assert.NotNull(result);
        Assert.Equal(2, result.Count);
    }
}
