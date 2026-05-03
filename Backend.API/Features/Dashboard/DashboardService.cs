using Backend.Database;
using Backend.Features.Accesses;
using Backend.Features.Vehicles;
using Microsoft.EntityFrameworkCore;

namespace Backend.Features.Dashboard;

public interface IDashboardService
{
    Task<OccupancyDto> GetOccupancyAsync();
    Task<DashboardMetricsDto> GetMetricsAsync();
}

public class DashboardService(AppDbContext db) : IDashboardService
{
    private readonly AppDbContext _db = db;

    public async Task<OccupancyDto> GetOccupancyAsync()
    {
        var vehicles = await _db.Vehicles
            .AsNoTracking()
            .Include(v => v.User)
            .Where(v => v.TagId != null && _db.Accesses
                .Where(a => a.TagId == v.TagId)
                .OrderByDescending(a => a.Timestamp)
                .Select(a => a.Type)
                .FirstOrDefault() == AccessType.Entry)
            .ToListAsync();

        var vehicleDtos = vehicles.Select(VehicleDto.FromModel).ToList();

        return new OccupancyDto
        {
            CurrentOccupancy = vehicleDtos.Count,
            Vehicles = vehicleDtos
        };
    }

    public async Task<DashboardMetricsDto> GetMetricsAsync()
    {
        var now = DateTime.UtcNow;
        var oneHourAgo = now.AddHours(-1);
        var twentyFourHoursAgo = now.AddHours(-24);

        var entriesLastHour = await _db.Accesses
            .CountAsync(a => a.Type == AccessType.Entry && a.Timestamp >= oneHourAgo);

        var exitsLastHour = await _db.Accesses
            .CountAsync(a => a.Type == AccessType.Exit && a.Timestamp >= oneHourAgo);

        var peakEntryTime = await _db.Accesses
            .Where(a => a.Type == AccessType.Entry && a.Timestamp >= twentyFourHoursAgo)
            .GroupBy(a => a.Timestamp.Hour)
            .OrderByDescending(g => g.Count())
            .Select(g => g.Key)
            .FirstOrDefaultAsync();

        return new DashboardMetricsDto
        {
            EntriesLastHour = entriesLastHour,
            ExitsLastHour = exitsLastHour,
            PeakEntryTime = peakEntryTime == 0 && entriesLastHour == 0
                ? null
                : $"{peakEntryTime:D2}:00"
        };
    }
}
