using System.Net;
using System.Net.Http.Json;
using Backend.Database;
using Backend.Features.Accesses;
using Backend.Features.Tags;
using Backend.Features.Tags.Enums;
using Backend.Features.Users;
using Backend.Features.Vehicles;
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

    private async Task SeedVehicleAndTagAsync(string tagId)
    {
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var user = new User { Name = "Test", Email = $"test_{Guid.NewGuid()}@example.com", PasswordHash = "hash", Role = UserRole.Customer };
        db.Users.Add(user);

        var tag = new Tag { TagId = tagId, Status = TagStatus.IN_USE };
        db.Tags.Add(tag);

        db.Vehicles.Add(new Vehicle { UserId = user.UserId, TagId = tagId, Plate = $"TST{Guid.NewGuid().ToString()[..4]}", Brand = "VW", Model = "Gol" });

        await db.SaveChangesAsync();
    }

    [Fact]
    public async Task PostEntry_WithValidTag_ReturnsCreated()
    {

        var tagId = "VALID-TAG";
        await SeedVehicleAndTagAsync(tagId);
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

        var tagId = "DOUBLE-ENTRY-TAG";
        await SeedVehicleAndTagAsync(tagId);
        var payload = new CreateAccessDto { TagId = tagId };


        await _client.PostAsync("/api/accesses/entry", JsonContent.Create(payload, options: CustomWebApplicationFactory.JsonOptions));


        var response = await _client.PostAsync("/api/accesses/entry", JsonContent.Create(payload, options: CustomWebApplicationFactory.JsonOptions));


        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task PostExit_WhenSuccessfullyEntered_ReturnsCreated()
    {

        var tagId = "FULL-CYCLE-TAG";
        await SeedVehicleAndTagAsync(tagId);
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

        var tagId = "EXIT-ONLY-TAG";
        await SeedVehicleAndTagAsync(tagId);
        var payload = new CreateAccessDto { TagId = tagId };


        var response = await _client.PostAsync("/api/accesses/exit", JsonContent.Create(payload, options: CustomWebApplicationFactory.JsonOptions));


        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task PostEntry_WithNonExistentTag_ReturnsNotFound()
    {

        var payload = new CreateAccessDto { TagId = "GHOST-TAG" };


        var response = await _client.PostAsync("/api/accesses/entry", JsonContent.Create(payload, options: CustomWebApplicationFactory.JsonOptions));


        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}
