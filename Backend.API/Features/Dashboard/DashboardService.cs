using Backend.Database;
using Backend.Features.Accesses;
using Backend.Features.Vehicles;
using Backend.Features.Settings;
using Microsoft.EntityFrameworkCore;

namespace Backend.Features.Dashboard;

public interface IDashboardService
{
    Task<OccupancyDto> GetOccupancyAsync();
    Task<DashboardMetricsDto> GetMetricsAsync();
    Task<DashboardMetricsDto> GetDashboardAsync();
}

public class DashboardService(AppDbContext db, ISettingsService settingsService) : IDashboardService
{
    private readonly AppDbContext _db = db;
    private readonly ISettingsService _settingsService = settingsService;

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
        var maxOccupancy = await _settingsService.GetAsync("max_occupancy", 100);
        var occupancyPercentage = maxOccupancy == 0
            ? 0.0
            : Math.Round((double)vehicleDtos.Count / maxOccupancy * 100, 1);

        return new OccupancyDto
        {
            CurrentOccupancy = vehicleDtos.Count,
            MaxOccupancy = maxOccupancy,
            OccupancyPercentage = occupancyPercentage,
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

        var maxOccupancy = await _settingsService.GetAsync("max_occupancy", 100);

        return new DashboardMetricsDto
        {
            EntriesLastHour = entriesLastHour,
            ExitsLastHour = exitsLastHour,
            PeakEntryTime = peakEntryTime == 0 && entriesLastHour == 0
                ? null
                : $"{peakEntryTime:D2}:00",
            MaxOccupancy = maxOccupancy
        };
    }

    public async Task<DashboardMetricsDto> GetDashboardAsync()
    {
        var now = DateTime.UtcNow;
        var oneHourAgo = now.AddHours(-1);
        var twentyFourHoursAgo = now.AddHours(-24);

        var entriesTask = _db.Accesses
            .CountAsync(a => a.Type == AccessType.Entry && a.Timestamp >= oneHourAgo);

        var exitsTask = _db.Accesses
            .CountAsync(a => a.Type == AccessType.Exit && a.Timestamp >= oneHourAgo);

        var peakGroupTask = _db.Accesses
            .Where(a => a.Type == AccessType.Entry && a.Timestamp >= twentyFourHoursAgo)
            .GroupBy(a => a.Timestamp.Hour)
            .OrderByDescending(g => g.Count())
            .Select(g => new { Hour = g.Key, Count = g.Count() })
            .FirstOrDefaultAsync();

        var currentOccupancyTask = _db.Vehicles
            .AsNoTracking()
            .Where(v => v.TagId != null && _db.Accesses
                .Where(a => a.TagId == v.TagId)
                .OrderByDescending(a => a.Timestamp)
                .Select(a => a.Type)
                .FirstOrDefault() == AccessType.Entry)
            .CountAsync();

        var accessesTask = _db.Accesses
            .AsNoTracking()
            .Where(a => a.Timestamp >= twentyFourHoursAgo)
            .OrderByDescending(a => a.Timestamp)
            .ToListAsync();

        var maxOccupancyTask = _settingsService.GetAsync("max_occupancy", 100);

        await Task.WhenAll(entriesTask, exitsTask, peakGroupTask, currentOccupancyTask, accessesTask, maxOccupancyTask);

        var peakGroup = await peakGroupTask;

        return new DashboardMetricsDto
        {
            EntriesLastHour = await entriesTask,
            ExitsLastHour = await exitsTask,
            PeakEntryTime = peakGroup != null ? $"{peakGroup.Hour:D2}:00" : null,
            PeakEntryHour = peakGroup?.Count ?? 0,
            CurrentOccupancy = await currentOccupancyTask,
            MaxOccupancy = await maxOccupancyTask,
            Accesses = (await accessesTask).Select(AccessDto.FromModel),
            UpdatedAt = now
        };
    }
}
