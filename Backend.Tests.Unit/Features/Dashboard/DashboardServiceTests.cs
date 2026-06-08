using Backend.Database;
using Backend.Features.Accesses;
using Backend.Features.Dashboard;
using Backend.Features.Tags;
using Backend.Features.Settings;
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
}