using Backend.Database;
using Backend.Features.Tags;
using Backend.Features.Users;
using Backend.Features.Vehicles;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using tests.Setup;

namespace tests.Features.Tags;

public class TagDbConstraintTests : IClassFixture<CustomWebApplicationFactory>, IAsyncLifetime
{
    private readonly CustomWebApplicationFactory _factory;

    public TagDbConstraintTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
        factory.CreateClient();
    }

    public async Task InitializeAsync() => await _factory.ResetDatabaseAsync();
    public Task DisposeAsync() => Task.CompletedTask;

    private AppDbContext CreateDbContext()
    {
        var scope = _factory.Services.CreateScope();
        return scope.ServiceProvider.GetRequiredService<AppDbContext>();
    }

    private static async Task<Vehicle> CreateVehicleAsync(AppDbContext db, string suffix = "")
    {
        var user = new User
        {
            Name = "Test User",
            Email = $"user{suffix}@test.com",
            PasswordHash = "hash",
            Cpf = $"{suffix.PadLeft(11, '0')}",
            PhoneNumber = $"{suffix.PadLeft(16, '0')}",
        };
        db.Users.Add(user);
        await db.SaveChangesAsync();

        var vehicle = new Vehicle
        {
            UserId = user.UserId,
            Plate = $"PLT{suffix}",
            Brand = "Honda",
            Model = "Civic",
            Color = "White",
        };
        db.Vehicles.Add(vehicle);
        await db.SaveChangesAsync();

        return vehicle;
    }

    [Fact]
    public async Task CreateTag_WithoutVehicle_ShouldPersist()
    {
        var db = CreateDbContext();

        var tag = new Tag { Status = TagStatus.Available };
        db.Tags.Add(tag);
        await db.SaveChangesAsync();

        var persisted = await db.Tags.FindAsync(tag.TagId);
        Assert.NotNull(persisted);
        Assert.Null(persisted.VeichleId);
        Assert.Equal(TagStatus.Available, persisted.Status);
    }

    [Fact]
    public async Task AssignTag_ToVehicle_WithStatusInUse_ShouldPersist()
    {
        var db = CreateDbContext();
        var vehicle = await CreateVehicleAsync(db, "10");

        var tag = new Tag { VeichleId = vehicle.VehicleId, Status = TagStatus.InUse };
        db.Tags.Add(tag);
        await db.SaveChangesAsync();

        var persisted = await db.Tags.FindAsync(tag.TagId);
        Assert.NotNull(persisted);
        Assert.Equal(vehicle.VehicleId, persisted.VeichleId);
        Assert.Equal(TagStatus.InUse, persisted.Status);
    }

    [Fact]
    public async Task AssignSecondInUseTag_ToSameVehicle_ShouldThrow()
    {
        var db = CreateDbContext();
        var vehicle = await CreateVehicleAsync(db, "20");

        db.Tags.Add(new Tag { VeichleId = vehicle.VehicleId, Status = TagStatus.InUse });
        await db.SaveChangesAsync();

        db.Tags.Add(new Tag { VeichleId = vehicle.VehicleId, Status = TagStatus.InUse });
        await Assert.ThrowsAsync<DbUpdateException>(() => db.SaveChangesAsync());
    }

    [Fact]
    public async Task AssignTwoTags_ToSameVehicle_WithDifferentStatuses_ShouldPersist()
    {
        var db = CreateDbContext();
        var vehicle = await CreateVehicleAsync(db, "30");

        db.Tags.Add(new Tag { VeichleId = vehicle.VehicleId, Status = TagStatus.InUse });
        db.Tags.Add(new Tag { VeichleId = vehicle.VehicleId, Status = TagStatus.Inactive });
        await db.SaveChangesAsync();

        var tags = await db.Tags.Where(t => t.VeichleId == vehicle.VehicleId).ToListAsync();
        Assert.Equal(2, tags.Count);
    }

    [Fact]
    public async Task DeleteVehicle_ShouldSetTagVehicleIdToNull()
    {
        var db = CreateDbContext();
        var vehicle = await CreateVehicleAsync(db, "40");

        var tag = new Tag { VeichleId = vehicle.VehicleId, Status = TagStatus.InUse };
        db.Tags.Add(tag);
        await db.SaveChangesAsync();

        db.Vehicles.Remove(vehicle);
        await db.SaveChangesAsync();

        var updated = await db.Tags.FindAsync(tag.TagId);
        Assert.NotNull(updated);
        Assert.Null(updated.VeichleId);
    }
}
