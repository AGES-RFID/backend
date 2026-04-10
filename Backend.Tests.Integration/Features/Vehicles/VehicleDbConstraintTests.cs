using Backend.Database;
using Backend.Features.Users;
using Backend.Features.Vehicles;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using tests.Setup;

namespace tests.Features.Vehicles;

public class VehicleDbConstraintTests : IClassFixture<CustomWebApplicationFactory>, IAsyncLifetime
{
    private readonly CustomWebApplicationFactory _factory;

    public VehicleDbConstraintTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
        // Triggers ConfigureWebHost and runs migrations before any test executes
        factory.CreateClient();
    }

    public async Task InitializeAsync() => await _factory.ResetDatabaseAsync();
    public Task DisposeAsync() => Task.CompletedTask;

    private AppDbContext CreateDbContext()
    {
        var scope = _factory.Services.CreateScope();
        return scope.ServiceProvider.GetRequiredService<AppDbContext>();
    }

    private static User BuildUser(string suffix = "") => new()
    {
        Name = "Test User",
        Role = UserRole.Client,
        Email = $"user{suffix}@test.com",
        PasswordHash = "hash",
        Cpf = $"{suffix.PadLeft(11, '0')}",
        PhoneNumber = $"{suffix.PadLeft(16, '0')}",
    };

    private static Vehicle BuildVehicle(Guid userId, string plate = "ABC1234") => new()
    {
        UserId = userId,
        Plate = plate,
        Brand = "Toyota",
        Model = "Corolla",
        Color = "Black",
    };

    [Fact]
    public async Task CreateVehicle_WithExistingUser_ShouldPersist()
    {
        var db = CreateDbContext();

        var user = BuildUser("1");
        db.Users.Add(user);
        await db.SaveChangesAsync();

        var vehicle = BuildVehicle(user.UserId);
        db.Vehicles.Add(vehicle);
        await db.SaveChangesAsync();

        var persisted = await db.Vehicles.FindAsync(vehicle.VehicleId);
        Assert.NotNull(persisted);
        Assert.Equal(user.UserId, persisted.UserId);
        Assert.Equal("ABC1234", persisted.Plate);
    }

    [Fact]
    public async Task CreateVehicle_WithDuplicatePlate_ShouldThrow()
    {
        var db = CreateDbContext();

        var user = BuildUser("2");
        db.Users.Add(user);
        await db.SaveChangesAsync();

        db.Vehicles.Add(BuildVehicle(user.UserId, "DUP0001"));
        await db.SaveChangesAsync();

        db.Vehicles.Add(BuildVehicle(user.UserId, "DUP0001"));
        await Assert.ThrowsAsync<DbUpdateException>(() => db.SaveChangesAsync());
    }

    [Fact]
    public async Task DeleteUser_ShouldCascadeDeleteVehicles()
    {
        var db = CreateDbContext();

        var user = BuildUser("3");
        db.Users.Add(user);
        await db.SaveChangesAsync();

        db.Vehicles.Add(BuildVehicle(user.UserId, "CAS0001"));
        db.Vehicles.Add(BuildVehicle(user.UserId, "CAS0002"));
        await db.SaveChangesAsync();

        db.Users.Remove(user);
        await db.SaveChangesAsync();

        var remaining = await db.Vehicles.Where(v => v.UserId == user.UserId).ToListAsync();
        Assert.Empty(remaining);
    }
}
