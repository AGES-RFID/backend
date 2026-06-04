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
    private readonly IServiceScopeFactory _scopeFactory = factory.Services.GetRequiredService<IServiceScopeFactory>();

    public async Task InitializeAsync()
    {
        await factory.ResetDatabaseAsync();
    }

    public Task DisposeAsync() => Task.CompletedTask;

    private async Task SeedUserAsync(User user)
    {
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        db.Users.Add(user);
        await db.SaveChangesAsync();
    }

    private async Task SeedVehicleAsync(Vehicle vehicle)
    {
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        db.Vehicles.Add(vehicle);
        await db.SaveChangesAsync();
    }

    private async Task SeedTagAsync(Tag tag)
    {
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        if (tag.Vehicle != null)
        {
            db.Attach(tag.Vehicle);
        }

        db.Tags.Add(tag);
        await db.SaveChangesAsync();
    }

    public static IEnumerable<object[]> ProtectedTagEndpoints()
    {
        yield return ["GET", "/api/tags"];
        yield return ["POST", "/api/tags"];
        yield return ["PATCH", $"/api/tags/{Guid.NewGuid()}/deactivate"];
        yield return ["PATCH", $"/api/tags/{Guid.NewGuid()}/assign-vehicle"];
    }

    [Theory]
    [MemberData(nameof(ProtectedTagEndpoints))]
    public async Task ProtectedEndpoints_WhenNotAuthenticated_ReturnUnauthorized(string method, string path)
    {
        var anonymousClient = AuthTestHelper.CreateAnonymousClient(factory);

        HttpResponseMessage response = method switch
        {
            "GET" => await anonymousClient.GetAsync(path),
            "POST" => await anonymousClient.PostAsync(path, JsonContent.Create(new { })),
            "PATCH" => await anonymousClient.PatchAsync(path, JsonContent.Create(new { })),
            _ => throw new InvalidOperationException($"Unsupported HTTP method: {method}")
        };

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Theory]
    [MemberData(nameof(ProtectedTagEndpoints))]
    public async Task ProtectedEndpoints_WhenCustomerAuthenticated_ReturnForbidden(string method, string path)
    {
        var customerClient = await AuthTestHelper.CreateClientAsAsync(factory, UserRole.Customer);

        HttpResponseMessage response = method switch
        {
            "GET" => await customerClient.GetAsync(path),
            "POST" => await customerClient.PostAsync(path, JsonContent.Create(new { })),
            "PATCH" => await customerClient.PatchAsync(path, JsonContent.Create(new { })),
            _ => throw new InvalidOperationException($"Unsupported HTTP method: {method}")
        };

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task CreateTag_ShouldReturnCreatedTag()
    {
        var adminClient = await AuthTestHelper.CreateClientAsAsync(factory, UserRole.Admin);
        var newTag = new CreateTagDto { Epc = "EPC-001", Tid = "TID-001" };

        var response = await adminClient.PostAsync("/api/tags", JsonContent.Create(newTag));

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var createdTag = await response.Content.ReadFromJsonAsync<TagDto>();
        Assert.NotNull(createdTag);
        Assert.NotEqual(Guid.Empty, createdTag.TagId);
        Assert.Equal("AVAILABLE", createdTag.Status);
        Assert.Equal(Guid.Empty, createdTag.VehicleId ?? Guid.Empty);
    }

    [Fact]
    public async Task GetAllTags_ShouldReturnCreatedTag()
    {
        var adminClient = await AuthTestHelper.CreateClientAsAsync(factory, UserRole.Admin);
        var newTag = new CreateTagDto { Epc = "EPC-002", Tid = "TID-002" };

        var createResponse = await adminClient.PostAsync("/api/tags", JsonContent.Create(newTag));
        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);

        var response = await adminClient.GetAsync("/api/tags");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var tags = await response.Content.ReadFromJsonAsync<List<TagListDto>>();
        Assert.NotNull(tags);

        var tag = Assert.Single(tags, t => t.Epc == newTag.Epc);
        Assert.Equal("AVAILABLE", tag.Status);
    }

    [Fact]
    public async Task GetAllTags_WithStatusFilter_ShouldReturnOnlyMatchingTags()
    {
        var adminClient = await AuthTestHelper.CreateClientAsAsync(factory, UserRole.Admin);
        var availableTag = new Tag { Status = Backend.Features.Tags.Enums.TagStatus.AVAILABLE, Epc = "EPC-012", Tid = "TID-012" };
        var inactiveTag = new Tag { Status = Backend.Features.Tags.Enums.TagStatus.INACTIVE, Epc = "EPC-013", Tid = "TID-013" };

        await SeedTagAsync(availableTag);
        await SeedTagAsync(inactiveTag);

        var response = await adminClient.GetAsync("/api/tags?status=AVAILABLE");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var tags = await response.Content.ReadFromJsonAsync<List<TagListDto>>();
        Assert.NotNull(tags);

        Assert.Single(tags);
        Assert.Equal(availableTag.TagId, tags[0].TagId);
        Assert.Equal("AVAILABLE", tags[0].Status);
    }

    [Fact]
    public async Task CreateTag_WhenTagAlreadyExists_ShouldReturnConflict()
    {
        var adminClient = await AuthTestHelper.CreateClientAsAsync(factory, UserRole.Admin);
        var newTag = new CreateTagDto { Epc = "EPC-003", Tid = "TID-003" };

        var firstResponse = await adminClient.PostAsync("/api/tags", JsonContent.Create(newTag));
        Assert.Equal(HttpStatusCode.Created, firstResponse.StatusCode);

        var secondResponse = await adminClient.PostAsync("/api/tags", JsonContent.Create(newTag));

        Assert.Equal(HttpStatusCode.Conflict, secondResponse.StatusCode);
    }

    [Fact]
    public async Task GetAllTags_WithInvalidStatus_ShouldReturnBadRequest()
    {
        var adminClient = await AuthTestHelper.CreateClientAsAsync(factory, UserRole.Admin);

        var response = await adminClient.GetAsync("/api/tags?status=INVALID");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task DeactivateTag_ShouldReturnNotFound_WhenTagDoesNotExist()
    {
        var adminClient = await AuthTestHelper.CreateClientAsAsync(factory, UserRole.Admin);

        var response = await adminClient.PatchAsync($"/api/tags/{Guid.NewGuid()}/deactivate", null);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task DeactivateTag_ShouldDeactivateExistingTag()
    {
        var adminClient = await AuthTestHelper.CreateClientAsAsync(factory, UserRole.Admin);
        var seeded = new Tag { Status = Backend.Features.Tags.Enums.TagStatus.AVAILABLE, Epc = "EPC-005", Tid = "TID-005" };
        await SeedTagAsync(seeded);

        var response = await adminClient.PatchAsync($"/api/tags/{seeded.TagId}/deactivate", null);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var tag = await response.Content.ReadFromJsonAsync<TagDto>();
        Assert.NotNull(tag);
        Assert.Equal(seeded.TagId, tag.TagId);
        Assert.Equal("INACTIVE", tag.Status);
    }

    [Fact]
    public async Task AssignVehicle_ShouldAssignVehicleToTag()
    {
        var adminClient = await AuthTestHelper.CreateClientAsAsync(factory, UserRole.Admin);
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

        var seeded = new Tag { Status = Backend.Features.Tags.Enums.TagStatus.AVAILABLE, Epc = "EPC-006", Tid = "TID-006" };
        await SeedTagAsync(seeded);

        var response = await adminClient.PatchAsJsonAsync($"/api/tags/{seeded.TagId}/assign-vehicle", new AssignVehicleDto { VehicleId = vehicle.VehicleId });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var tag = await response.Content.ReadFromJsonAsync<TagDto>();
        Assert.NotNull(tag);
        Assert.Equal(seeded.TagId, tag.TagId);
        Assert.Equal("IN_USE", tag.Status);
        Assert.Equal(vehicle.VehicleId, tag.VehicleId);
    }

    [Fact]
    public async Task AssignVehicle_ShouldReturnConflict_WhenVehicleAlreadyHasActiveTag()
    {
        var adminClient = await AuthTestHelper.CreateClientAsAsync(factory, UserRole.Admin);
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

        var firstTag = new Tag { Status = Backend.Features.Tags.Enums.TagStatus.AVAILABLE, Epc = "EPC-007", Tid = "TID-007" };
        var secondTag = new Tag { Status = Backend.Features.Tags.Enums.TagStatus.AVAILABLE, Epc = "EPC-008", Tid = "TID-008" };

        await SeedTagAsync(firstTag);
        await SeedTagAsync(secondTag);

        var firstResponse = await adminClient.PatchAsJsonAsync($"/api/tags/{firstTag.TagId}/assign-vehicle", new AssignVehicleDto { VehicleId = vehicle.VehicleId });
        Assert.Equal(HttpStatusCode.OK, firstResponse.StatusCode);

        var secondResponse = await adminClient.PatchAsJsonAsync($"/api/tags/{secondTag.TagId}/assign-vehicle", new AssignVehicleDto { VehicleId = vehicle.VehicleId });

        Assert.Equal(HttpStatusCode.Conflict, secondResponse.StatusCode);
    }

    [Fact]
    public async Task AssignVehicle_ShouldReturnConflict_WhenTagIsAlreadyAssigned()
    {
        var adminClient = await AuthTestHelper.CreateClientAsAsync(factory, UserRole.Admin);
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

        var seeded = new Tag
        {
            Status = Backend.Features.Tags.Enums.TagStatus.IN_USE,
            Epc = "EPC-009",
            Tid = "TID-009",
            Vehicle = vehicle
        };
        await SeedTagAsync(seeded);

        var response = await adminClient.PatchAsJsonAsync($"/api/tags/{seeded.TagId}/assign-vehicle", new AssignVehicleDto { VehicleId = vehicle.VehicleId });

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task AssignVehicle_ShouldReturnConflict_WhenTagIsInactive()
    {
        var adminClient = await AuthTestHelper.CreateClientAsAsync(factory, UserRole.Admin);
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

        var seeded = new Tag
        {
            Status = Backend.Features.Tags.Enums.TagStatus.INACTIVE,
            Epc = "EPC-010",
            Tid = "TID-010"
        };
        await SeedTagAsync(seeded);

        var response = await adminClient.PatchAsJsonAsync($"/api/tags/{seeded.TagId}/assign-vehicle", new AssignVehicleDto { VehicleId = vehicle.VehicleId });

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task AssignVehicle_ShouldReturnNotFound_WhenTagDoesNotExist()
    {
        var adminClient = await AuthTestHelper.CreateClientAsAsync(factory, UserRole.Admin);
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

        var response = await adminClient.PatchAsJsonAsync($"/api/tags/{Guid.NewGuid()}/assign-vehicle", new AssignVehicleDto { VehicleId = vehicle.VehicleId });

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task AssignVehicle_ShouldReturnNotFound_WhenVehicleDoesNotExist()
    {
        var adminClient = await AuthTestHelper.CreateClientAsAsync(factory, UserRole.Admin);
        var seeded = new Tag { Status = Backend.Features.Tags.Enums.TagStatus.AVAILABLE, Epc = "EPC-011", Tid = "TID-011" };
        await SeedTagAsync(seeded);

        var response = await adminClient.PatchAsJsonAsync($"/api/tags/{seeded.TagId}/assign-vehicle", new AssignVehicleDto { VehicleId = Guid.NewGuid() });

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}
