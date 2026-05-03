using Backend.Database;
using Backend.Features.Accesses;
using Backend.Features.Dashboard;
using Backend.Features.Tags;
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

    private static Tag CreateTag(string tagId = "TAG001") => new()
    {
        TagId = tagId
    };

    private static Access CreateAccess(string tagId, AccessType type, DateTime timestamp) => new()
    {
        TagId = tagId,
        Type = type,
        Timestamp = timestamp,
        Tag = new Tag { TagId = tagId }
    };

    [Fact]
    public async Task GetMetricsAsync_WhenNoAccesses_ReturnsZerosAndNullPeakTime()
    {
        var db = CreateInMemoryDb();
        var service = new DashboardService(db);

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

        db.Accesses.Add(CreateAccess("TAG001", AccessType.Entry, now.AddMinutes(-10)));
        db.Accesses.Add(CreateAccess("TAG002", AccessType.Entry, now.AddMinutes(-30)));
        db.Accesses.Add(CreateAccess("TAG003", AccessType.Entry, now.AddHours(-2)));
        await db.SaveChangesAsync();

        var service = new DashboardService(db);
        var result = await service.GetMetricsAsync();

        Assert.Equal(2, result.EntriesLastHour);
    }

    [Fact]
    public async Task GetMetricsAsync_WhenExitsInLastHour_ReturnsCorrectCount()
    {
        var db = CreateInMemoryDb();
        var now = DateTime.UtcNow;

        db.Accesses.Add(CreateAccess("TAG001", AccessType.Exit, now.AddMinutes(-15)));
        db.Accesses.Add(CreateAccess("TAG002", AccessType.Exit, now.AddMinutes(-45)));
        db.Accesses.Add(CreateAccess("TAG003", AccessType.Exit, now.AddHours(-3)));
        await db.SaveChangesAsync();

        var service = new DashboardService(db);
        var result = await service.GetMetricsAsync();

        Assert.Equal(2, result.ExitsLastHour);
    }

    [Fact]
    public async Task GetMetricsAsync_WhenEntriesInLast24h_ReturnsPeakEntryTime()
    {
        var db = CreateInMemoryDb();
        var now = DateTime.UtcNow;
        var peakHour = new DateTime(now.Year, now.Month, now.Day, now.Hour, 0, 0, DateTimeKind.Utc).AddHours(-2);

        db.Accesses.Add(CreateAccess("TAG001", AccessType.Entry, peakHour));
        db.Accesses.Add(CreateAccess("TAG002", AccessType.Entry, peakHour.AddMinutes(10)));
        db.Accesses.Add(CreateAccess("TAG003", AccessType.Entry, peakHour.AddMinutes(20)));
        db.Accesses.Add(CreateAccess("TAG004", AccessType.Entry, now.AddMinutes(-5)));
        await db.SaveChangesAsync();

        var service = new DashboardService(db);
        var result = await service.GetMetricsAsync();

        Assert.NotNull(result.PeakEntryTime);
        Assert.Equal($"{peakHour.Hour:D2}:00", result.PeakEntryTime);
    }

    [Fact]
    public async Task GetMetricsAsync_ExitsDoNotCountAsEntries()
    {
        var db = CreateInMemoryDb();
        var now = DateTime.UtcNow;

        db.Accesses.Add(CreateAccess("TAG001", AccessType.Exit, now.AddMinutes(-10)));
        db.Accesses.Add(CreateAccess("TAG002", AccessType.Exit, now.AddMinutes(-20)));
        await db.SaveChangesAsync();

        var service = new DashboardService(db);
        var result = await service.GetMetricsAsync();

        Assert.Equal(0, result.EntriesLastHour);
        Assert.Equal(2, result.ExitsLastHour);
    }

    [Fact]
    public async Task GetMetricsAsync_EntriesOlderThan24h_NotCountedInPeakTime()
    {
        var db = CreateInMemoryDb();
        var now = DateTime.UtcNow;

        db.Accesses.Add(CreateAccess("TAG001", AccessType.Entry, now.AddHours(-25)));
        db.Accesses.Add(CreateAccess("TAG002", AccessType.Entry, now.AddHours(-30)));
        await db.SaveChangesAsync();

        var service = new DashboardService(db);
        var result = await service.GetMetricsAsync();

        Assert.Null(result.PeakEntryTime);
    }
}