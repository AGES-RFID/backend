using Backend.Database;
using Backend.Features.Auth;
using Backend.Features.Tags;
using Backend.Features.Tags.Enums;
using Backend.Features.Users;
using Backend.Features.Vehicles;
using Microsoft.EntityFrameworkCore;
using NSubstitute;

namespace Backend.Tests.Unit.Features.Vehicles;

public class VehicleServiceTests
{
    [Fact]
    public async Task CreateVehicleAsync_WhenAvailableTagExists_AssignsTagToNewVehicle()
    {
        await using var db = CreateDbContext();
        var user = await SeedUserAsync(db);
        var tag = await SeedTagAsync(db);
        var service = CreateService(db, user.UserId, UserRole.Admin);

        var result = await service.CreateVehicleAsync(new CreateVehicleDto
        {
            Plate = "AAA9A99",
            Brand = "Honda",
            Model = "HRV",
            UserId = user.UserId
        });

        var savedTag = await db.Tags.SingleAsync(t => t.TagId == tag.TagId);

        Assert.Equal(tag.TagId, result.TagId);
        Assert.Equal(TagStatus.IN_USE, savedTag.Status);
    }

    [Fact]
    public async Task CreateVehicleAsync_WhenExistingVehicleHasNoTag_AssignsAvailableTagToIt()
    {
        await using var db = CreateDbContext();
        var user = await SeedUserAsync(db);
        var oldVehicle = await SeedVehicleWithoutTagAsync(db, user.UserId);
        await SeedTagAsync(db, epc: "EPC-001", tid: "TID-001");
        await SeedTagAsync(db, epc: "EPC-002", tid: "TID-002");
        var service = CreateService(db, user.UserId, UserRole.Admin);

        var newVehicle = await service.CreateVehicleAsync(new CreateVehicleDto
        {
            Plate = "NEW9A99",
            Brand = "Toyota",
            Model = "Corolla",
            UserId = user.UserId
        });

        var reloadedOldVehicle = await db.Vehicles.SingleAsync(v => v.VehicleId == oldVehicle.VehicleId);
        var tags = await db.Tags.ToListAsync();

        Assert.NotNull(newVehicle.TagId);
        Assert.NotNull(reloadedOldVehicle.TagId);
        Assert.All(tags, tag => Assert.Equal(TagStatus.IN_USE, tag.Status));
    }

    private static AppDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new AppDbContext(options);
    }

    private static VehicleService CreateService(AppDbContext db, Guid userId, UserRole role)
    {
        var currentUserContext = Substitute.For<ICurrentUserContext>();
        currentUserContext.GetRequiredUserId().Returns(userId);
        currentUserContext.GetRequiredRole().Returns(role);

        return new VehicleService(db, currentUserContext);
    }

    private static async Task<User> SeedUserAsync(AppDbContext db)
    {
        var user = new User
        {
            Name = "Test User",
            Email = $"{Guid.NewGuid()}@example.com",
            PasswordHash = "hash",
            Role = UserRole.Admin
        };

        db.Users.Add(user);
        await db.SaveChangesAsync();
        return user;
    }

    private static async Task<Tag> SeedTagAsync(
        AppDbContext db,
        string epc = "EPC-001",
        string tid = "TID-001")
    {
        var tag = new Tag
        {
            Epc = epc,
            Tid = tid,
            Status = TagStatus.AVAILABLE
        };

        db.Tags.Add(tag);
        await db.SaveChangesAsync();
        return tag;
    }

    private static async Task<Vehicle> SeedVehicleWithoutTagAsync(AppDbContext db, Guid userId)
    {
        var vehicle = new Vehicle
        {
            UserId = userId,
            Plate = "OLD9A99",
            Brand = "Ford",
            Model = "Ka"
        };

        db.Vehicles.Add(vehicle);
        await db.SaveChangesAsync();
        return vehicle;
    }
}
