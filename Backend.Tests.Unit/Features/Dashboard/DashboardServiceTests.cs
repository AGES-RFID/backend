using Backend.Database;
using Backend.Features.Accesses;
using Backend.Features.Dashboard;
using Backend.Features.Tags;
using Backend.Features.Settings;
using Backend.Features.Users;
using Backend.Features.Vehicles;
using Microsoft.EntityFrameworkCore;

namespace Backend.Tests.Unit.Features.Dashboard;

public class DashboardServiceTests
{
    private static AppDbContext CreateInMemoryDb()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        return new AppDbContext(options);
    }

    private static DashboardService CreateService(AppDbContext db)
        => new(db, new SettingsService(db));

    private static Access CreateAccess(Guid tagId, AccessType type, DateTime timestamp) => new()
    {
        TagId = tagId,
        Type = type,
        Timestamp = timestamp,
        Tag = new Tag { TagId = tagId, Epc = $"EPC-{tagId}", Tid = $"TID-{tagId}" }
    };

    [Fact]
    public async Task GetMetricsAsync_WhenNoAccesses_ReturnsZerosAndNullPeakTime()
    {
        var db = CreateInMemoryDb();
        var service = CreateService(db);

        var result = await service.GetMetricsAsync();

        Assert.Equal(0, result.EntriesLastHour);
        Assert.Equal(0, result.PeakHourEntries);
        Assert.Equal(0, result.ExitsLastHour);
        Assert.Null(result.PeakEntryTime);
    }

    [Fact]
    public async Task GetMetricsAsync_WhenEntriesInLastHour_ReturnsCorrectCount()
    {
        var db = CreateInMemoryDb();
        var now = DateTime.UtcNow;

        db.Accesses.Add(CreateAccess(Guid.NewGuid(), AccessType.Entry, now.AddMinutes(-10)));
        db.Accesses.Add(CreateAccess(Guid.NewGuid(), AccessType.Entry, now.AddMinutes(-30)));
        db.Accesses.Add(CreateAccess(Guid.NewGuid(), AccessType.Entry, now.AddHours(-2)));
        await db.SaveChangesAsync();

        var service = CreateService(db);
        var result = await service.GetMetricsAsync();

        Assert.Equal(2, result.EntriesLastHour);
    }

    [Fact]
    public async Task GetMetricsAsync_WhenExitsInLastHour_ReturnsCorrectCount()
    {
        var db = CreateInMemoryDb();
        var now = DateTime.UtcNow;

        db.Accesses.Add(CreateAccess(Guid.NewGuid(), AccessType.Exit, now.AddMinutes(-15)));
        db.Accesses.Add(CreateAccess(Guid.NewGuid(), AccessType.Exit, now.AddMinutes(-45)));
        db.Accesses.Add(CreateAccess(Guid.NewGuid(), AccessType.Exit, now.AddHours(-3)));
        await db.SaveChangesAsync();

        var service = CreateService(db);
        var result = await service.GetMetricsAsync();

        Assert.Equal(2, result.ExitsLastHour);
    }

    [Fact]
    public async Task GetMetricsAsync_WhenEntriesInLast24h_ReturnsPeakEntryTime()
    {
        var db = CreateInMemoryDb();
        var now = DateTime.UtcNow;
        var peakHour = new DateTime(now.Year, now.Month, now.Day, now.Hour, 0, 0, DateTimeKind.Utc).AddHours(-2);

        db.Accesses.Add(CreateAccess(Guid.NewGuid(), AccessType.Entry, peakHour));
        db.Accesses.Add(CreateAccess(Guid.NewGuid(), AccessType.Entry, peakHour.AddMinutes(10)));
        db.Accesses.Add(CreateAccess(Guid.NewGuid(), AccessType.Entry, peakHour.AddMinutes(20)));
        db.Accesses.Add(CreateAccess(Guid.NewGuid(), AccessType.Entry, now.AddMinutes(-5)));
        await db.SaveChangesAsync();

        var service = CreateService(db);
        var result = await service.GetMetricsAsync();

        Assert.NotNull(result.PeakEntryTime);
        Assert.Equal($"{peakHour.Hour:D2}:00", result.PeakEntryTime);
        Assert.Equal(3, result.PeakHourEntries);
    }

    [Fact]
    public async Task GetMetricsAsync_ExitsDoNotCountAsEntries()
    {
        var db = CreateInMemoryDb();
        var now = DateTime.UtcNow;

        db.Accesses.Add(CreateAccess(Guid.NewGuid(), AccessType.Exit, now.AddMinutes(-10)));
        db.Accesses.Add(CreateAccess(Guid.NewGuid(), AccessType.Exit, now.AddMinutes(-20)));
        await db.SaveChangesAsync();

        var service = CreateService(db);
        var result = await service.GetMetricsAsync();

        Assert.Equal(0, result.EntriesLastHour);
        Assert.Equal(2, result.ExitsLastHour);
        Assert.Equal(0, result.PeakHourEntries);
    }

    [Fact]
    public async Task GetMetricsAsync_EntriesOlderThan24h_NotCountedInPeakTime()
    {
        var db = CreateInMemoryDb();
        var now = DateTime.UtcNow;

        db.Accesses.Add(CreateAccess(Guid.NewGuid(), AccessType.Entry, now.AddHours(-25)));
        db.Accesses.Add(CreateAccess(Guid.NewGuid(), AccessType.Entry, now.AddHours(-30)));
        await db.SaveChangesAsync();

        var service = CreateService(db);
        var result = await service.GetMetricsAsync();

        Assert.Null(result.PeakEntryTime);
        Assert.Equal(0, result.PeakHourEntries);
    }

    [Fact]
    public async Task GetMetricsAsync_WhenSettingsExist_ReturnsMaxOccupancy()
    {
        var db = CreateInMemoryDb();
        db.Settings.Add(new Settings { Name = "max_occupancy", Value = "150" });
        await db.SaveChangesAsync();

        var result = await CreateService(db).GetMetricsAsync();

        Assert.Equal(150, result.MaxOccupancy);
    }

    [Fact]
    public async Task GetMetricsAsync_WhenNoSettings_ReturnsDefaultMaxOccupancy()
    {
        var db = CreateInMemoryDb();

        var result = await CreateService(db).GetMetricsAsync();

        Assert.Equal(100, result.MaxOccupancy);
    }



    private static async Task<(Guid tagId, Guid vehicleId)> SeedVehicleCurrentlyInsideAsync(AppDbContext db)
    {
        var tagId = Guid.NewGuid();
        var userId = Guid.NewGuid();

        db.Users.Add(new User
        {
            UserId = userId,
            Name = "Test",
            Email = $"{userId}@test.com",
            PasswordHash = "hash",
            Role = UserRole.Customer
        });
        db.Tags.Add(new Tag { TagId = tagId, Epc = $"EPC-{tagId}", Tid = $"TID-{tagId}" });
        await db.SaveChangesAsync();

        db.Vehicles.Add(new Vehicle
        {
            UserId = userId,
            TagId = tagId,
            Plate = $"AAA-{tagId.ToString()[..4]}",
            Brand = "Honda",
            Model = "Civic"
        });
        await db.SaveChangesAsync();

        var existingTag = await db.Tags.FindAsync(tagId) ?? throw new InvalidOperationException();
        db.Accesses.Add(new Access
        {
            TagId = tagId,
            Type = AccessType.Entry,
            Timestamp = DateTime.UtcNow.AddMinutes(-30),
            Tag = existingTag
        });
        await db.SaveChangesAsync();

        return (tagId, tagId);
    }

    [Fact]
    public async Task GetOccupancyAsync_WhenNoVehiclesInside_ReturnsZeroOccupancy()
    {
        var db = CreateInMemoryDb();

        var result = await CreateService(db).GetOccupancyAsync();

        Assert.Equal(0, result.CurrentOccupancy);
        Assert.Empty(result.Vehicles);
    }

    [Fact]
    public async Task GetOccupancyAsync_WhenSettingExists_ReturnsMaxOccupancy()
    {
        var db = CreateInMemoryDb();
        db.Settings.Add(new Settings { Name = "max_occupancy", Value = "200" });
        await db.SaveChangesAsync();

        var result = await CreateService(db).GetOccupancyAsync();

        Assert.Equal(200, result.MaxOccupancy);
    }

    [Fact]
    public async Task GetOccupancyAsync_WhenNoSettings_ReturnsDefaultMaxOccupancy()
    {
        var db = CreateInMemoryDb();

        var result = await CreateService(db).GetOccupancyAsync();

        Assert.Equal(100, result.MaxOccupancy);
    }

    [Fact]
    public async Task GetOccupancyAsync_WhenVehiclesInside_ReturnsCorrectPercentage()
    {
        var db = CreateInMemoryDb();
        db.Settings.Add(new Settings { Name = "max_occupancy", Value = "100" });
        await db.SaveChangesAsync();
        await SeedVehicleCurrentlyInsideAsync(db);

        var result = await CreateService(db).GetOccupancyAsync();

        Assert.Equal(1, result.CurrentOccupancy);
        Assert.Equal(1.0, result.OccupancyPercentage);
    }

    [Fact]
    public async Task GetOccupancyAsync_WhenMaxOccupancyIsZero_ReturnsZeroPercentage()
    {
        var db = CreateInMemoryDb();
        db.Settings.Add(new Settings { Name = "max_occupancy", Value = "0" });
        await db.SaveChangesAsync();

        var result = await CreateService(db).GetOccupancyAsync();

        Assert.Equal(0.0, result.OccupancyPercentage);
    }

    [Fact]
    public async Task GetDashboardAsync_ReturnsAggregatedData()
    {
        var db = CreateInMemoryDb();
        db.Settings.Add(new Settings { Name = "max_occupancy", Value = "100" });
        await db.SaveChangesAsync();
        await SeedVehicleCurrentlyInsideAsync(db);

        var service = CreateService(db);
        var result = await service.GetDashboardAsync();

        Assert.Equal(1, result.CurrentOccupancy);
        Assert.Equal(100, result.MaxOccupancy);
        Assert.Equal(1, result.EntriesLastHour);
        Assert.Equal(0, result.ExitsLastHour);
        Assert.True(result.PeakHourEntries > 0);
        Assert.NotNull(result.PeakEntryTime);
        Assert.Equal($"{DateTime.UtcNow.AddMinutes(-30).Hour:D2}:00", result.PeakEntryTime);
        Assert.True(result.UpdatedAt > DateTime.MinValue);
        Assert.NotNull(result.Accesses);
        Assert.NotEmpty(result.Accesses);
    }

    [Fact]
    public async Task GetPermanenceAsync_WhenNoVehiclesInside_ReturnsEmptyList()
    {
        var db = CreateInMemoryDb();
        var service = CreateService(db);

        var result = await service.GetPermanenceAsync();

        Assert.Empty(result);
    }

    [Fact]
    public async Task GetPermanenceAsync_WhenVehicleInside_ReturnsCorrectPermanence()
    {
        var db = CreateInMemoryDb();
        var now = DateTime.UtcNow;

        var tagId = Guid.NewGuid();
        var userId = Guid.NewGuid();

        db.Users.Add(new User
        {
            UserId = userId,
            Name = "Test",
            Email = $"{userId}@test.com",
            PasswordHash = "hash",
            Role = UserRole.Customer
        });
        db.Tags.Add(new Tag { TagId = tagId, Epc = "EPC-TEST-001", Tid = "TID-TEST-001" });
        db.Vehicles.Add(new Vehicle
        {
            UserId = userId,
            TagId = tagId,
            Plate = "ABC-1234",
            Brand = "Honda",
            Model = "Civic"
        });
        await db.SaveChangesAsync();

        var trackedTag = await db.Tags.FindAsync(tagId)
            ?? throw new InvalidOperationException();

        db.Accesses.Add(new Access
        {
            TagId = tagId,
            Type = AccessType.Entry,
            Timestamp = now.AddMinutes(-120),
            Tag = trackedTag
        });
        await db.SaveChangesAsync();

        var service = CreateService(db);
        var result = await service.GetPermanenceAsync();

        var permanence = Assert.Single(result);
        Assert.Equal("EPC-TEST-001", permanence.RfidTag);
        Assert.Equal("ABC-1234", permanence.Plate);
        Assert.InRange(permanence.MinutesParked, 119, 121);
    }

    [Fact]
    public async Task GetPermanenceAsync_WhenVehicleExited_NotIncluded()
    {
        var db = CreateInMemoryDb();
        var now = DateTime.UtcNow;

        var tagId = Guid.NewGuid();
        var userId = Guid.NewGuid();

        db.Users.Add(new User
        {
            UserId = userId,
            Name = "Test",
            Email = $"{userId}@test.com",
            PasswordHash = "hash",
            Role = UserRole.Customer
        });
        db.Tags.Add(new Tag { TagId = tagId, Epc = "EPC-EXIT-001", Tid = "TID-EXIT-001" });
        db.Vehicles.Add(new Vehicle
        {
            UserId = userId,
            TagId = tagId,
            Plate = "XYZ-5678",
            Brand = "Toyota",
            Model = "Corolla"
        });
        await db.SaveChangesAsync();

        var trackedTag = await db.Tags.FindAsync(tagId)
            ?? throw new InvalidOperationException();

        db.Accesses.Add(new Access
        {
            TagId = tagId,
            Type = AccessType.Entry,
            Timestamp = now.AddMinutes(-60),
            Tag = trackedTag
        });
        db.Accesses.Add(new Access
        {
            TagId = tagId,
            Type = AccessType.Exit,
            Timestamp = now.AddMinutes(-10),
            Tag = trackedTag
        });
        await db.SaveChangesAsync();
        var service = CreateService(db);
        var result = await service.GetPermanenceAsync();

        Assert.Empty(result);
    }

    [Fact]
    public async Task GetPermanenceAsync_WhenVehicleReentered_UsesLastEntry()
    {
        var db = CreateInMemoryDb();
        var now = DateTime.UtcNow;

        var tagId = Guid.NewGuid();
        var userId = Guid.NewGuid();

        db.Users.Add(new User
        {
            UserId = userId,
            Name = "Test",
            Email = $"{userId}@test.com",
            PasswordHash = "hash",
            Role = UserRole.Customer
        });
        db.Tags.Add(new Tag { TagId = tagId, Epc = "EPC-RE-001", Tid = "TID-RE-001" });
        db.Vehicles.Add(new Vehicle
        {
            UserId = userId,
            TagId = tagId,
            Plate = "RE-0001",
            Brand = "Fiat",
            Model = "Uno"
        });
        await db.SaveChangesAsync();

        var trackedTag = await db.Tags.FindAsync(tagId)
            ?? throw new InvalidOperationException();

        db.Accesses.Add(new Access
        {
            TagId = tagId,
            Type = AccessType.Entry,
            Timestamp = now.AddMinutes(-180),
            Tag = trackedTag
        });
        db.Accesses.Add(new Access
        {
            TagId = tagId,
            Type = AccessType.Exit,
            Timestamp = now.AddMinutes(-90),
            Tag = trackedTag
        });
        db.Accesses.Add(new Access
        {
            TagId = tagId,
            Type = AccessType.Entry,
            Timestamp = now.AddMinutes(-45),
            Tag = trackedTag
        });
        await db.SaveChangesAsync();

        var service = CreateService(db);
        var result = await service.GetPermanenceAsync();

        var permanence = Assert.Single(result);
        Assert.Equal("EPC-RE-001", permanence.RfidTag);
        Assert.Equal("RE-0001", permanence.Plate);
        Assert.InRange(permanence.MinutesParked, 44, 46);
    }

    [Fact]
    public async Task GetPermanenceAsync_WhenMultipleVehiclesInside_ReturnsAll()
    {
        var db = CreateInMemoryDb();
        var now = DateTime.UtcNow;

        var tagA = Guid.NewGuid();
        var tagB = Guid.NewGuid();
        var userId = Guid.NewGuid();

        db.Users.Add(new User
        {
            UserId = userId,
            Name = "Owner",
            Email = $"{userId}@test.com",
            PasswordHash = "hash",
            Role = UserRole.Customer
        });
        db.Tags.Add(new Tag { TagId = tagA, Epc = "EPC-A", Tid = "TID-A" });
        db.Tags.Add(new Tag { TagId = tagB, Epc = "EPC-B", Tid = "TID-B" });
        db.Vehicles.Add(new Vehicle { UserId = userId, TagId = tagA, Plate = "CAR-A", Brand = "A", Model = "A" });
        db.Vehicles.Add(new Vehicle { UserId = userId, TagId = tagB, Plate = "CAR-B", Brand = "B", Model = "B" });
        await db.SaveChangesAsync();

        var trackedTagA = await db.Tags.FindAsync(tagA)
            ?? throw new InvalidOperationException();
        var trackedTagB = await db.Tags.FindAsync(tagB)
            ?? throw new InvalidOperationException();

        db.Accesses.Add(new Access
        {
            TagId = tagA,
            Type = AccessType.Entry,
            Timestamp = now.AddMinutes(-30),
            Tag = trackedTagA
        });
        db.Accesses.Add(new Access
        {
            TagId = tagB,
            Type = AccessType.Entry,
            Timestamp = now.AddMinutes(-15),
            Tag = trackedTagB
        });
        await db.SaveChangesAsync();

        var service = CreateService(db);
        var result = await service.GetPermanenceAsync();

        Assert.Equal(2, result.Count());
        Assert.Contains(result, p => p.Plate == "CAR-A");
        Assert.Contains(result, p => p.Plate == "CAR-B");
    }

    [Fact]
    public async Task GetPermanenceAsync_VehicleWithoutTag_NotIncluded()
    {
        var db = CreateInMemoryDb();
        var userId = Guid.NewGuid();

        db.Users.Add(new User
        {
            UserId = userId,
            Name = "Test",
            Email = $"{userId}@test.com",
            PasswordHash = "hash",
            Role = UserRole.Customer
        });
        db.Vehicles.Add(new Vehicle
        {
            UserId = userId,
            TagId = null,
            Plate = "NO-TAG",
            Brand = "Test",
            Model = "Test"
        });
        await db.SaveChangesAsync();

        var service = CreateService(db);
        var result = await service.GetPermanenceAsync();

        Assert.Empty(result);
    }
}