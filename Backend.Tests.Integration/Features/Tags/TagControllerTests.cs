using System.Net;
using System.Net.Http.Json;
using Backend.Database;
using Backend.Features.Tags;
using Backend.Features.Users;
using Vehicle = Backend.Features.Vehicles.Vehicle;
using Microsoft.Extensions.DependencyInjection;
using tests.Setup;

namespace tests.Features.Tags;

public class TagControllerTests(CustomWebApplicationFactory factory) : IClassFixture<CustomWebApplicationFactory>, IAsyncLifetime
{
    private readonly HttpClient _client = factory.CreateClient();

    public async Task InitializeAsync()
    {
        await factory.ResetDatabaseAsync();
    }

    public Task DisposeAsync() => Task.CompletedTask;

    private async Task SeedUserAsync(User user)
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        db.Users.Add(user);
        await db.SaveChangesAsync();
    }

    private async Task SeedVehicleAsync(Vehicle vehicle)
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        db.Vehicles.Add(vehicle);
        await db.SaveChangesAsync();
    }

    private async Task SeedTagAsync(Tag tag)
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        if (tag.Vehicle != null)
        {
            db.Attach(tag.Vehicle);
        }

        db.Tags.Add(tag);
        await db.SaveChangesAsync();
    }

    [Fact]
    public async Task CreateTag_ShouldReturnCreatedTag()
    {
        var newTag = new CreateTagDto { TagId = "TAG-001" };

        var response = await _client.PostAsync("/api/tags", JsonContent.Create(newTag));

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var createdTag = await response.Content.ReadFromJsonAsync<TagDto>();
        Assert.NotNull(createdTag);
        Assert.Equal(newTag.TagId, createdTag.TagId);
        Assert.Equal("AVAILABLE", createdTag.Status);
        Assert.Equal(Guid.Empty, createdTag.VehicleId ?? Guid.Empty);
    }

    [Fact]
    public async Task GetAllTags_ShouldReturnCreatedTag()
    {
        var newTag = new CreateTagDto { TagId = "TAG-002" };

        var createResponse = await _client.PostAsync("/api/tags", JsonContent.Create(newTag));
        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);

        var response = await _client.GetAsync("/api/tags");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var tags = await response.Content.ReadFromJsonAsync<List<TagListDto>>();
        Assert.NotNull(tags);

        var tag = Assert.Single(tags, t => t.Id == newTag.TagId);
        Assert.Equal("AVAILABLE", tag.Status);
    }

    [Fact]
    public async Task GetAllTags_WithStatusFilter_ShouldReturnOnlyMatchingTags()
    {
        var availableTagId = "TAG-012";
        var inactiveTagId = "TAG-013";

        await SeedTagAsync(new Tag { TagId = availableTagId, Status = Backend.Features.Tags.Enums.TagStatus.AVAILABLE });
        await SeedTagAsync(new Tag { TagId = inactiveTagId, Status = Backend.Features.Tags.Enums.TagStatus.INACTIVE });

        var response = await _client.GetAsync("/api/tags?status=AVAILABLE");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var tags = await response.Content.ReadFromJsonAsync<List<TagListDto>>();
        Assert.NotNull(tags);

        Assert.Single(tags);
        Assert.Equal(availableTagId, tags[0].Id);
        Assert.Equal("AVAILABLE", tags[0].Status);
    }

    [Fact]
    public async Task CreateTag_WhenTagAlreadyExists_ShouldReturnConflict()
    {
        var newTag = new CreateTagDto { TagId = "TAG-003" };

        var firstResponse = await _client.PostAsync("/api/tags", JsonContent.Create(newTag));
        Assert.Equal(HttpStatusCode.Created, firstResponse.StatusCode);

        var secondResponse = await _client.PostAsync("/api/tags", JsonContent.Create(newTag));

        Assert.Equal(HttpStatusCode.Conflict, secondResponse.StatusCode);
    }

    [Fact]
    public async Task GetAllTags_WithInvalidStatus_ShouldReturnBadRequest()
    {
        var response = await _client.GetAsync("/api/tags?status=INVALID");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task DeactivateTag_ShouldReturnNotFound_WhenTagDoesNotExist()
    {
        var response = await _client.PatchAsync("/api/tags/TAG-404/deactivate", null);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task DeactivateTag_ShouldDeactivateExistingTag()
    {
        var tagId = "TAG-005";
        await SeedTagAsync(new Tag { TagId = tagId, Status = Backend.Features.Tags.Enums.TagStatus.AVAILABLE });

        var response = await _client.PatchAsync($"/api/tags/{tagId}/deactivate", null);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var tag = await response.Content.ReadFromJsonAsync<TagDto>();
        Assert.NotNull(tag);
        Assert.Equal(tagId, tag.TagId);
        Assert.Equal("INACTIVE", tag.Status);
    }

    [Fact]
    public async Task AssignVehicle_ShouldAssignVehicleToTag()
    {
        var user = new User { Name = "Alice", Email = "alice@example.com", PasswordHash = "hash", Role = UserRole.Admin };
        await SeedUserAsync(user);

        var vehicle = new Vehicle
        {
            Plate = "ABC1D23",
            Brand = "Honda",
            Model = "HRV",
            UserId = user.UserId
        };
        await SeedVehicleAsync(vehicle);

        var tagId = "TAG-006";
        await SeedTagAsync(new Tag { TagId = tagId, Status = Backend.Features.Tags.Enums.TagStatus.AVAILABLE });

        var response = await _client.PatchAsJsonAsync($"/api/tags/{tagId}/assign-vehicle", new AssignVehicleDto { VehicleId = vehicle.VehicleId });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var tag = await response.Content.ReadFromJsonAsync<TagDto>();
        Assert.NotNull(tag);
        Assert.Equal(tagId, tag.TagId);
        Assert.Equal("IN_USE", tag.Status);
        Assert.Equal(vehicle.VehicleId, tag.VehicleId);
    }

    [Fact]
    public async Task AssignVehicle_ShouldReturnConflict_WhenVehicleAlreadyHasActiveTag()
    {
        var user = new User { Name = "Bob", Email = "bob@example.com", PasswordHash = "hash", Role = UserRole.Admin };
        await SeedUserAsync(user);

        var vehicle = new Vehicle
        {
            Plate = "XYZ9Z99",
            Brand = "Toyota",
            Model = "Corolla",
            UserId = user.UserId
        };
        await SeedVehicleAsync(vehicle);

        var firstTagId = "TAG-007";
        var secondTagId = "TAG-008";

        await SeedTagAsync(new Tag { TagId = firstTagId, Status = Backend.Features.Tags.Enums.TagStatus.AVAILABLE });
        await SeedTagAsync(new Tag { TagId = secondTagId, Status = Backend.Features.Tags.Enums.TagStatus.AVAILABLE });

        var firstResponse = await _client.PatchAsJsonAsync($"/api/tags/{firstTagId}/assign-vehicle", new AssignVehicleDto { VehicleId = vehicle.VehicleId });
        Assert.Equal(HttpStatusCode.OK, firstResponse.StatusCode);

        var secondResponse = await _client.PatchAsJsonAsync($"/api/tags/{secondTagId}/assign-vehicle", new AssignVehicleDto { VehicleId = vehicle.VehicleId });

        Assert.Equal(HttpStatusCode.Conflict, secondResponse.StatusCode);
    }

    [Fact]
    public async Task AssignVehicle_ShouldReturnConflict_WhenTagIsAlreadyAssigned()
    {
        var user = new User { Name = "Carol", Email = "carol@example.com", PasswordHash = "hash", Role = UserRole.Admin };
        await SeedUserAsync(user);

        var vehicle = new Vehicle
        {
            Plate = "CAR1L23",
            Brand = "Ford",
            Model = "Fiesta",
            UserId = user.UserId
        };
        await SeedVehicleAsync(vehicle);

        var tagId = "TAG-009";
        await SeedTagAsync(new Tag
        {
            TagId = tagId,
            Status = Backend.Features.Tags.Enums.TagStatus.IN_USE,
            Vehicle = vehicle
        });

        var response = await _client.PatchAsJsonAsync($"/api/tags/{tagId}/assign-vehicle", new AssignVehicleDto { VehicleId = vehicle.VehicleId });

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task AssignVehicle_ShouldReturnConflict_WhenTagIsInactive()
    {
        var user = new User { Name = "Dana", Email = "dana@example.com", PasswordHash = "hash", Role = UserRole.Admin };
        await SeedUserAsync(user);

        var vehicle = new Vehicle
        {
            Plate = "DAN2A34",
            Brand = "Chevrolet",
            Model = "Onix",
            UserId = user.UserId
        };
        await SeedVehicleAsync(vehicle);

        var tagId = "TAG-010";
        await SeedTagAsync(new Tag
        {
            TagId = tagId,
            Status = Backend.Features.Tags.Enums.TagStatus.INACTIVE
        });

        var response = await _client.PatchAsJsonAsync($"/api/tags/{tagId}/assign-vehicle", new AssignVehicleDto { VehicleId = vehicle.VehicleId });

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task AssignVehicle_ShouldReturnNotFound_WhenTagDoesNotExist()
    {
        var user = new User { Name = "Eva", Email = "eva@example.com", PasswordHash = "hash", Role = UserRole.Admin };
        await SeedUserAsync(user);

        var vehicle = new Vehicle
        {
            Plate = "EVA3A45",
            Brand = "Volkswagen",
            Model = "T-Cross",
            UserId = user.UserId
        };
        await SeedVehicleAsync(vehicle);

        var response = await _client.PatchAsJsonAsync("/api/tags/TAG-404/assign-vehicle", new AssignVehicleDto { VehicleId = vehicle.VehicleId });

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task AssignVehicle_ShouldReturnNotFound_WhenVehicleDoesNotExist()
    {
        var tagId = "TAG-011";
        await SeedTagAsync(new Tag { TagId = tagId, Status = Backend.Features.Tags.Enums.TagStatus.AVAILABLE });

        var response = await _client.PatchAsJsonAsync($"/api/tags/{tagId}/assign-vehicle", new AssignVehicleDto { VehicleId = Guid.NewGuid() });

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}