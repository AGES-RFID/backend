using Backend.Database;
using Backend.Features.Tags;
using Backend.Features.Tags.Enums;
using Backend.Features.Users;
using Backend.Features.Vehicles;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Http;

namespace Backend.Tests.Unit.Features.Tags;

public class TagServiceTests
{
    [Fact]
    public async Task CreateTagAsync_WhenVehicleWithoutTagExists_AssignsTagToVehicle()
    {
        await using var db = CreateDbContext();
        var user = await SeedUserAsync(db);
        var vehicle = await SeedVehicleWithoutTagAsync(db, user.UserId);
        var service = new TagService(db);

        var result = await service.CreateTagAsync(new CreateTagDto
        {
            Epc = "EPC-001",
            Tid = "TID-001"
        });

        var reloadedVehicle = await db.Vehicles.SingleAsync(v => v.VehicleId == vehicle.VehicleId);

        Assert.Equal("IN_USE", result.Status);
        Assert.Equal(vehicle.VehicleId, result.VehicleId);
        Assert.Equal(result.TagId, reloadedVehicle.TagId);
    }

    [Fact]
    public async Task CreateTagsFromCsvAsync_WhenCsvHasHeader_CreatesTags()
    {
        await using var db = CreateDbContext();
        var service = new TagService(db);
        var file = CreateCsvFile("tid,epc\nTID-001,EPC-001\nTID-002,EPC-002");

        var result = await service.CreateTagsFromCsvAsync(file);

        Assert.Equal(2, result.CreatedCount);
        Assert.Empty(result.Errors);
        Assert.Equal(2, await db.Tags.CountAsync());
    }

    [Fact]
    public async Task CreateTagsFromCsvAsync_WhenLineIsDuplicate_ReturnsErrorAndKeepsValidRows()
    {
        await using var db = CreateDbContext();
        var service = new TagService(db);
        await service.CreateTagAsync(new CreateTagDto { Tid = "TID-001", Epc = "EPC-001" });
        var file = CreateCsvFile("tid,epc\nTID-001,EPC-999\nTID-002,EPC-002");

        var result = await service.CreateTagsFromCsvAsync(file);

        Assert.Equal(1, result.CreatedCount);
        Assert.Single(result.Errors);
        Assert.Equal(2, await db.Tags.CountAsync());
    }

    private static AppDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new AppDbContext(options);
    }

    private static IFormFile CreateCsvFile(string content)
    {
        var bytes = System.Text.Encoding.UTF8.GetBytes(content);
        var stream = new MemoryStream(bytes);
        return new FormFile(stream, 0, bytes.Length, "file", "tags.csv")
        {
            Headers = new HeaderDictionary(),
            ContentType = "text/csv"
        };
    }

    private static async Task<User> SeedUserAsync(AppDbContext db)
    {
        var user = new User
        {
            Name = "Test User",
            Email = $"{Guid.NewGuid()}@example.com",
            PasswordHash = "hash",
            Role = UserRole.Customer
        };

        db.Users.Add(user);
        await db.SaveChangesAsync();
        return user;
    }

    private static async Task<Vehicle> SeedVehicleWithoutTagAsync(AppDbContext db, Guid userId)
    {
        var vehicle = new Vehicle
        {
            UserId = userId,
            Plate = "CAR9A99",
            Brand = "Honda",
            Model = "HRV"
        };

        db.Vehicles.Add(vehicle);
        await db.SaveChangesAsync();
        return vehicle;
    }
}
