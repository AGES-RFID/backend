using Backend.Database;
using Backend.Features.Accesses;
using Backend.Features.Tags;
using Backend.Features.Tags.Enums;
using Backend.Features.Vehicles;
using Microsoft.EntityFrameworkCore;

namespace Backend.Tests.Unit.Features.Accesses;

public class AccessesServiceTests
{
    private static AppDbContext CreateInMemoryDb()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        return new AppDbContext(options);
    }

    private static async Task<(Tag Tag, Vehicle Vehicle)> SeedTagAndVehicle(AppDbContext db, TagStatus status = TagStatus.IN_USE)
    {
        var tag = new Tag { Status = status, Epc = $"EPC-{Guid.NewGuid()}", Tid = $"TID-{Guid.NewGuid()}" };
        var vehicle = new Vehicle { UserId = Guid.NewGuid(), TagId = tag.TagId, Plate = $"TST-{Guid.NewGuid().ToString()[..4]}", Brand = "VW", Model = "Gol" };

        db.Tags.Add(tag);
        db.Vehicles.Add(vehicle);
        await db.SaveChangesAsync();

        return (tag, vehicle);
    }

    [Fact]
    public async Task RegisterAccessAsync_Entry_WithValidTag_CreatesAccess()
    {
        var db = CreateInMemoryDb();
        var sut = new AccessesService(db);
        var (tag, _) = await SeedTagAndVehicle(db);

        var dto = new CreateAccessDto { Tid = tag.Tid, Epc = tag.Epc, Entrance = true };

        var result = await sut.RegisterAccessAsync(dto);

        Assert.NotNull(result);
        Assert.Equal(AccessType.Entry, result.Type);
        Assert.Equal(tag.TagId, result.TagId);

        var saved = await db.Accesses.SingleAsync();
        Assert.Equal(AccessType.Entry, saved.Type);
    }

    [Fact]
    public async Task RegisterAccessAsync_Entry_WhenAlreadyInside_ThrowsAccessRegistrationConflictException()
    {
        var db = CreateInMemoryDb();
        var sut = new AccessesService(db);
        var (tag, _) = await SeedTagAndVehicle(db);

        db.Accesses.Add(new Access { TagId = tag.TagId, Tag = tag, Type = AccessType.Entry, Timestamp = DateTime.UtcNow });
        await db.SaveChangesAsync();

        var dto = new CreateAccessDto { Tid = tag.Tid, Epc = tag.Epc, Entrance = true };

        var exception = await Assert.ThrowsAsync<AccessRegistrationConflictException>(() => sut.RegisterAccessAsync(dto));
        Assert.Equal("tag_already_inside", exception.Reason);
    }

    [Fact]
    public async Task RegisterAccessAsync_Exit_WhenOutside_ThrowsAccessRegistrationConflictException()
    {
        var db = CreateInMemoryDb();
        var sut = new AccessesService(db);
        var (tag, _) = await SeedTagAndVehicle(db);

        var dto = new CreateAccessDto { Tid = tag.Tid, Epc = tag.Epc, Entrance = false };

        var exception = await Assert.ThrowsAsync<AccessRegistrationConflictException>(() => sut.RegisterAccessAsync(dto));
        Assert.Equal("tag_already_outside", exception.Reason);
    }

    [Fact]
    public async Task RegisterAccessAsync_WithInactiveTag_ThrowsInvalidOperationException()
    {
        var db = CreateInMemoryDb();
        var sut = new AccessesService(db);
        var (tag, _) = await SeedTagAndVehicle(db, TagStatus.INACTIVE);

        var dto = new CreateAccessDto { Tid = tag.Tid, Epc = tag.Epc, Entrance = true };

        await Assert.ThrowsAsync<InvalidOperationException>(() => sut.RegisterAccessAsync(dto));
    }

    [Fact]
    public async Task RegisterAccessAsync_WithNonExistentTag_ThrowsKeyNotFoundException()
    {
        var db = CreateInMemoryDb();
        var sut = new AccessesService(db);

        var dto = new CreateAccessDto { Tid = "NON-EXISTENT", Epc = "NON-EXISTENT", Entrance = true };

        await Assert.ThrowsAsync<KeyNotFoundException>(() => sut.RegisterAccessAsync(dto));
    }

    [Fact]
    public async Task GetTimeSeriesAsync_GroupsAccessesByHour()
    {
        var db = CreateInMemoryDb();
        var sut = new AccessesService(db);
        var (tag, _) = await SeedTagAndVehicle(db);

        var nowUTC = DateTime.UtcNow;
        var now = new DateTime(nowUTC.Year, nowUTC.Month, nowUTC.Day, nowUTC.Hour, 15, 0, DateTimeKind.Utc);
        var twoHoursAgo = now.AddHours(-2);
        var oneHourAgo = now.AddHours(-1);

        db.Accesses.AddRange(
            new Access { TagId = tag.TagId, Tag = tag, Type = AccessType.Entry, Timestamp = twoHoursAgo },
            new Access { TagId = tag.TagId, Tag = tag, Type = AccessType.Entry, Timestamp = twoHoursAgo.AddMinutes(10) },
            new Access { TagId = tag.TagId, Tag = tag, Type = AccessType.Exit, Timestamp = oneHourAgo }
        );
        await db.SaveChangesAsync();

        var result = await sut.GetTimeSeriesAsync();

        Assert.NotNull(result);
        Assert.Equal(2, result.Series.Count());

        var entries = result.Series.First(s => s.Key == "entries");
        var exits = result.Series.First(s => s.Key == "exits");

        Assert.Equal(24, entries.Points.Count());
        Assert.Equal(24, exits.Points.Count());

        var twoHoursAgoTruncated = new DateTime(twoHoursAgo.Year, twoHoursAgo.Month, twoHoursAgo.Day, twoHoursAgo.Hour, 0, 0, DateTimeKind.Utc);
        var oneHourAgoTruncated = new DateTime(oneHourAgo.Year, oneHourAgo.Month, oneHourAgo.Day, oneHourAgo.Hour, 0, 0, DateTimeKind.Utc);

        var entryPointTwoHoursAgo = entries.Points.FirstOrDefault(p => p.Timestamp == twoHoursAgoTruncated);
        var exitPointOneHourAgo = exits.Points.FirstOrDefault(p => p.Timestamp == oneHourAgoTruncated);

        Assert.NotNull(entryPointTwoHoursAgo);
        Assert.Equal(2, entryPointTwoHoursAgo.Count);

        Assert.NotNull(exitPointOneHourAgo);
        Assert.Equal(1, exitPointOneHourAgo.Count);
    }
    [Fact]
    public async Task GetAccessesAsync_ReturnsAccessesWithVehiclePlate()
    {
        var db = CreateInMemoryDb();
        var sut = new AccessesService(db);
        var (tag, vehicle) = await SeedTagAndVehicle(db);

        db.Accesses.Add(new Access { TagId = tag.TagId, Tag = tag, Type = AccessType.Entry, Timestamp = DateTime.UtcNow });
        await db.SaveChangesAsync();

        var result = await sut.GetAccessesAsync();

        Assert.NotNull(result);
        var accessDto = Assert.Single(result);
        Assert.Equal(vehicle.Plate, accessDto.Plate);
        Assert.Equal(AccessType.Entry, accessDto.Type);
    }
}
