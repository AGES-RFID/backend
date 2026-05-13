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

    private async Task<Guid> SeedVehicleAndTagAsync(TagStatus status = TagStatus.IN_USE)
    {
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var user = new User { Name = "Test", Email = $"test_{Guid.NewGuid()}@example.com", PasswordHash = "hash", Role = UserRole.Customer };
        db.Users.Add(user);

        var tag = new Tag { Status = status, Epc = $"EPC-{Guid.NewGuid()}", Tid = $"TID-{Guid.NewGuid()}" };
        db.Tags.Add(tag);

        db.Vehicles.Add(new Vehicle { UserId = user.UserId, TagId = tag.TagId, Plate = $"TST{Guid.NewGuid().ToString()[..4].ToUpper()}", Brand = "VW", Model = "Gol" });

        await db.SaveChangesAsync();
        return tag.TagId;
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
    public async Task PostEntry_WithValidTag_ReturnsCreated()
    {
        var tagId = await SeedVehicleAndTagAsync();
        var payload = new CreateAccessDto { TagId = tagId };


        var response = await _client.PostAsync("/api/accesses/entry", JsonContent.Create(payload, options: CustomWebApplicationFactory.JsonOptions));


        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<AccessDto>(CustomWebApplicationFactory.JsonOptions);
        Assert.NotNull(result);
        Assert.Equal(tagId, result.TagId);
        Assert.Equal(AccessType.Entry, result.Type);
    }

    [Fact]
    public async Task PostEntry_WhenAlreadyInside_ReturnsConflict()
    {
        var tagId = await SeedVehicleAndTagAsync();
        var payload = new CreateAccessDto { TagId = tagId };


        await _client.PostAsync("/api/accesses/entry", JsonContent.Create(payload, options: CustomWebApplicationFactory.JsonOptions));


        var response = await _client.PostAsync("/api/accesses/entry", JsonContent.Create(payload, options: CustomWebApplicationFactory.JsonOptions));


        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task PostExit_WhenSuccessfullyEntered_ReturnsCreated()
    {
        var tagId = await SeedVehicleAndTagAsync();
        var payload = new CreateAccessDto { TagId = tagId };


        await _client.PostAsync("/api/accesses/entry", JsonContent.Create(payload, options: CustomWebApplicationFactory.JsonOptions));


        var response = await _client.PostAsync("/api/accesses/exit", JsonContent.Create(payload, options: CustomWebApplicationFactory.JsonOptions));


        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<AccessDto>(CustomWebApplicationFactory.JsonOptions);
        Assert.Equal(AccessType.Exit, result?.Type);
    }

    [Fact]
    public async Task PostExit_WithoutPriorEntry_ReturnsConflict()
    {
        var tagId = await SeedVehicleAndTagAsync();
        var payload = new CreateAccessDto { TagId = tagId };


        var response = await _client.PostAsync("/api/accesses/exit", JsonContent.Create(payload, options: CustomWebApplicationFactory.JsonOptions));


        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task PostEntry_WithNonExistentTag_ReturnsNotFound()
    {
        var payload = new CreateAccessDto { TagId = Guid.NewGuid() };


        var response = await _client.PostAsync("/api/accesses/entry", JsonContent.Create(payload, options: CustomWebApplicationFactory.JsonOptions));


        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task PostEntry_WithInactiveTag_ReturnsConflict()
    {
        var tagId = await SeedVehicleAndTagAsync(TagStatus.INACTIVE);
        var payload = new CreateAccessDto { TagId = tagId };


        var response = await _client.PostAsync("/api/accesses/entry", JsonContent.Create(payload, options: CustomWebApplicationFactory.JsonOptions));


        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task PostExit_WithInactiveTag_ReturnsConflict()
    {
        var tagId = await SeedVehicleAndTagAsync(TagStatus.INACTIVE);
        await SeedAccessAsync(tagId, AccessType.Entry);
        var payload = new CreateAccessDto { TagId = tagId };


        var response = await _client.PostAsync("/api/accesses/exit", JsonContent.Create(payload, options: CustomWebApplicationFactory.JsonOptions));


        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }
}
