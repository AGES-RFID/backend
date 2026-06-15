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

        var peakEntry = await _db.Accesses
            .AsNoTracking()
            .Where(a => a.Type == AccessType.Entry && a.Timestamp >= twentyFourHoursAgo)
            .GroupBy(a => a.Timestamp.Hour)
            .OrderByDescending(g => g.Count())
            .Select(g => new { Hour = g.Key, Count = g.Count() })
            .FirstOrDefaultAsync();

        var maxOccupancy = await _settingsService.GetAsync("max_occupancy", 100);

        return new DashboardMetricsDto
        {
            EntriesLastHour = entriesLastHour,
            ExitsLastHour = exitsLastHour,
            PeakEntryTime = peakEntry != null
                ? $"{peakEntry.Hour:D2}:00"
                : null,
            PeakHourEntries = peakEntry?.Count ?? 0,
            MaxOccupancy = maxOccupancy
        };
    }

    public async Task<DashboardMetricsDto> GetDashboardAsync()
    {
        var now = DateTime.UtcNow;
        var twentyFourHoursAgo = now.AddHours(-24);

        var metricsTask = GetMetricsAsync();
        var occupancyTask = GetOccupancyAsync();
        var accessesTask = _db.Accesses
            .AsNoTracking()
            .Where(a => a.Timestamp >= twentyFourHoursAgo)
            .OrderByDescending(a => a.Timestamp)
            .Select(a => AccessDto.FromModel(a))
            .ToListAsync();

        await Task.WhenAll(metricsTask, occupancyTask, accessesTask);

        var metrics = await metricsTask;
        var occupancy = await occupancyTask;
        var accesses = await accessesTask;

        metrics.CurrentOccupancy = occupancy.CurrentOccupancy;
        metrics.MaxOccupancy = occupancy.MaxOccupancy;
        metrics.Accesses = accesses;
        metrics.UpdatedAt = now;

        return metrics;
    }
}
