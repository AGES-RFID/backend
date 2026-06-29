using Backend.Database;
using Backend.Features.Dashboard;
using Backend.Features.GatewayStatus;
using Backend.Features.SystemConfig.Models;
using Microsoft.EntityFrameworkCore;

namespace Backend.Features.SystemConfig;

public class SystemService(
    IDashboardService dashboardService,
    IConfiguration configuration,
    AppDbContext db) : ISystemService
{
    private static readonly Guid DefaultReaderId = Guid.Parse("00000000-0000-0000-0000-000000000001");
    private const double DefaultSensitivity = -70;
    private const double MinSensitivity = -93;
    private const double MaxSensitivity = -30;
    private readonly IDashboardService _dashboardService = dashboardService;
    private readonly IConfiguration _configuration = configuration;
    private readonly AppDbContext _db = db;

    public async Task<SystemDto> GetSystemAsync()
    {
        var occupancy = await _dashboardService.GetOccupancyAsync();
        var antennas = await GetAntennasAsync();

        return new SystemDto
        {
            OccupancyLimit = occupancy.MaxOccupancy,
            CurrentOccupancy = occupancy.CurrentOccupancy,
            Antennas = antennas
        };
    }

    public async Task<List<AntennaDto>> GetAntennasAsync()
    {
        var readerStatus = await GetLatestReaderStatusAsync(asNoTracking: false);

        if (readerStatus is not null)
        {
            if (readerStatus.AntennaList.Count == 0)
                await InitializeAntennasAsync(readerStatus);

            return readerStatus.AntennaList
                .OrderBy(a => a.Port)
                .Select(ToDto)
                .ToList();
        }

        return GetConfiguredAntennas();
    }

    public async Task<AntennaDto> UpdateAntennaAsync(Guid antennaId, UpdateAntennaDto dto)
    {
        var port = GetPortFromAntennaId(antennaId);
        var readerStatus = await GetLatestReaderStatusAsync(asNoTracking: false);

        if (readerStatus is null)
        {
            readerStatus = new ReaderStatus
            {
                ReaderId = DefaultReaderId,
                ReaderStatusValue = "disconnected",
                LastPing = DateTime.UtcNow,
                AntennaList = GetConfiguredReaderAntennas()
            };

            _db.ReaderStatuses.Add(readerStatus);
        }
        else if (readerStatus.AntennaList.Count == 0)
        {
            await InitializeAntennasAsync(readerStatus);
        }

        var antenna = readerStatus.AntennaList.FirstOrDefault(a => a.Port == port)
            ?? throw new KeyNotFoundException($"Antenna with id {antennaId} not found");

        antenna.AntennaStatus = ToAntennaStatus(dto.Status);

        if (dto.Power.HasValue)
            antenna.Power = dto.Power.Value;

        if (dto.Sensibility.HasValue)
            antenna.Sensitivity = NormalizeSensitivity(dto.Sensibility);

        readerStatus.LastPing = DateTime.UtcNow;

        await _db.SaveChangesAsync();

        return ToDto(antenna);
    }

    private async Task<ReaderStatus?> GetLatestReaderStatusAsync(bool asNoTracking)
    {
        var query = _db.ReaderStatuses
            .OrderByDescending(r => r.LastPing)
            .AsQueryable();

        if (asNoTracking)
            query = query.AsNoTracking();

        return await query.FirstOrDefaultAsync();
    }

    private async Task InitializeAntennasAsync(ReaderStatus readerStatus)
    {
        var configuredAntennas = GetConfiguredReaderAntennas();
        if (configuredAntennas.Count == 0)
            return;

        readerStatus.AntennaList = configuredAntennas;
        readerStatus.LastPing = DateTime.UtcNow;
        await _db.SaveChangesAsync();
    }

    private List<AntennaDto> GetConfiguredAntennas()
    {
        try
        {
            var cfg = _configuration.GetSection("Antennas").Get<List<AntennaConfig>>();
            if (cfg is null)
                return [];

            return cfg.Select(a => new AntennaDto
            {
                Id = GetAntennaId(a.Number),
                Name = $"Antena {a.Number}",
                Number = a.Number,
                Status = NormalizeUiStatus(a.Status),
                Sensibility = NormalizeSensitivity(a.Sensibility),
                Power = a.Power
            }).ToList();
        }
        catch
        {
            return [];
        }
    }

    private List<ReaderAntennaStatus> GetConfiguredReaderAntennas() =>
        GetConfiguredAntennas()
            .Select(a => new ReaderAntennaStatus
            {
                Port = a.Number,
                Power = a.Power ?? 0,
                Sensitivity = NormalizeSensitivity(a.Sensibility),
                AntennaStatus = ToAntennaStatus(a.Status)
            })
            .ToList();

    private static AntennaDto ToDto(ReaderAntennaStatus antenna) =>
        new()
        {
            Id = GetAntennaId(antenna.Port),
            Name = $"Antena {antenna.Port}",
            Number = antenna.Port,
            Status = string.Equals(antenna.AntennaStatus, "connected", StringComparison.OrdinalIgnoreCase) ? "On" : "Off",
            Sensibility = NormalizeSensitivity(antenna.Sensitivity),
            Power = antenna.Power
        };

    private static Guid GetAntennaId(int port) =>
        Guid.Parse($"00000000-0000-0000-0000-{port:000000000000}");

    private static int GetPortFromAntennaId(Guid antennaId)
    {
        var portText = antennaId.ToString().Split('-')[^1];
        return int.TryParse(portText, out var port) ? port : -1;
    }

    private static string ToAntennaStatus(string? status) =>
        string.Equals(status, "On", StringComparison.OrdinalIgnoreCase) ||
        string.Equals(status, "connected", StringComparison.OrdinalIgnoreCase)
            ? "connected"
            : "disconnected";

    private static string NormalizeUiStatus(string? status) =>
        string.Equals(status, "On", StringComparison.OrdinalIgnoreCase) ||
        string.Equals(status, "connected", StringComparison.OrdinalIgnoreCase)
            ? "On"
            : "Off";

    private static double NormalizeSensitivity(double? sensitivity)
    {
        if (!sensitivity.HasValue || sensitivity.Value == 0)
            return DefaultSensitivity;

        var normalized = sensitivity.Value > 0 ? -sensitivity.Value : sensitivity.Value;
        return Math.Clamp(normalized, MinSensitivity, MaxSensitivity);
    }
}
