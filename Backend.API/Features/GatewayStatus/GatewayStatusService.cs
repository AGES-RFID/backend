using Backend.Database;
using Microsoft.EntityFrameworkCore;

namespace Backend.Features.GatewayStatus;

public sealed class GatewayStatusService(AppDbContext db) : IGatewayStatusService
{
    private static readonly Guid DefaultReaderId = Guid.Parse("00000000-0000-0000-0000-000000000001");
    private const double DefaultSensitivity = -70;
    private const double MinSensitivity = -93;
    private const double MaxSensitivity = -30;
    private readonly AppDbContext _db = db;

    public async Task<ReaderStatusResponseDto> SaveStatusAsync(ReaderStatusDto status)
    {
        var readerId = status.ReaderId ?? DefaultReaderId;
        var lastPing = DateTime.UtcNow;
        var readerStatusValue = status.Connected ? "connected" : "disconnected";

        var readerStatus = await _db.ReaderStatuses
            .FirstOrDefaultAsync(r => r.ReaderId == readerId);

        if (readerStatus is null)
        {
            readerStatus = new ReaderStatus
            {
                ReaderId = readerId,
                AntennaList = ToAntennaList(status.Antennas)
            };
            await _db.ReaderStatuses.AddAsync(readerStatus);
        }

        var desiredAntennas = GetDesiredAntennas(readerStatus, status);
        readerStatus.ReaderStatusValue = readerStatusValue;
        readerStatus.LastPing = lastPing;

        if (desiredAntennas.Count == 0 && status.Antennas.Count > 0)
            readerStatus.AntennaList = ToAntennaList(status.Antennas);

        await _db.SaveChangesAsync();

        return ToResponse(readerStatus, desiredAntennas);
    }

    public async Task<ReaderStatusResponseDto?> GetLastStatusAsync()
    {
        var readerStatus = await _db.ReaderStatuses
            .AsNoTracking()
            .OrderByDescending(r => r.LastPing)
            .FirstOrDefaultAsync();

        return readerStatus is null ? null : ToResponse(readerStatus);
    }

    public async Task<ReaderStatusResponseDto> ConfirmConfigurationAsync(ReaderStatusDto status)
    {
        var readerId = status.ReaderId ?? DefaultReaderId;
        var readerStatus = await _db.ReaderStatuses
            .FirstOrDefaultAsync(r => r.ReaderId == readerId);

        if (readerStatus is null)
        {
            readerStatus = new ReaderStatus { ReaderId = readerId };
            await _db.ReaderStatuses.AddAsync(readerStatus);
        }

        readerStatus.ReaderStatusValue = status.Connected ? "connected" : "disconnected";
        readerStatus.LastPing = DateTime.UtcNow;
        MergeConfirmedAntennas(readerStatus, status.Antennas);

        await _db.SaveChangesAsync();

        return ToResponse(readerStatus);
    }

    private static List<AntennaStatusDto> GetDesiredAntennas(ReaderStatus savedStatus, ReaderStatusDto currentStatus)
    {
        var currentByPort = currentStatus.Antennas.ToDictionary(a => (int)a.Port);

        return savedStatus.AntennaList
            .OrderBy(a => a.Port)
            .Where(saved =>
                !currentByPort.TryGetValue(saved.Port, out var current) ||
                !AntennaMatches(saved, current))
            .Select(ToDto)
            .ToList();
    }

    private static bool AntennaMatches(ReaderAntennaStatus saved, AntennaStatusDto current) =>
        saved.Port == current.Port &&
        string.Equals(saved.AntennaStatus, current.AntennaStatus, StringComparison.OrdinalIgnoreCase) &&
        saved.Power == current.Power &&
        NormalizeSensitivity(saved.Sensitivity) == NormalizeSensitivity(current.Sensitivity);

    private static List<ReaderAntennaStatus> ToAntennaList(IEnumerable<AntennaStatusDto> antennas) =>
        antennas
            .OrderBy(a => a.Port)
            .Select(a => new ReaderAntennaStatus
            {
                Port = a.Port,
                Power = a.Power,
                Sensitivity = NormalizeSensitivity(a.Sensitivity),
                AntennaStatus = a.AntennaStatus
            })
            .ToList();

    private static void MergeConfirmedAntennas(ReaderStatus readerStatus, IEnumerable<AntennaStatusDto> antennas)
    {
        foreach (var confirmed in antennas.OrderBy(a => a.Port))
        {
            var saved = readerStatus.AntennaList.FirstOrDefault(a => a.Port == confirmed.Port);
            if (saved is null)
            {
                readerStatus.AntennaList.Add(new ReaderAntennaStatus
                {
                    Port = confirmed.Port,
                    Power = confirmed.Power,
                    Sensitivity = NormalizeSensitivity(confirmed.Sensitivity),
                    AntennaStatus = confirmed.AntennaStatus
                });
                continue;
            }

            saved.Power = confirmed.Power;
            saved.Sensitivity = NormalizeSensitivity(confirmed.Sensitivity);
            saved.AntennaStatus = confirmed.AntennaStatus;
        }
    }

    private static AntennaStatusDto ToDto(ReaderAntennaStatus antenna) =>
        new()
        {
            Port = (ushort)antenna.Port,
            Connected = string.Equals(antenna.AntennaStatus, "connected", StringComparison.OrdinalIgnoreCase),
            Power = antenna.Power,
            Sensitivity = NormalizeSensitivity(antenna.Sensitivity)
        };

    private static ReaderStatusResponseDto ToResponse(
        ReaderStatus readerStatus,
        IReadOnlyList<AntennaStatusDto>? desiredAntennas = null) =>
        new()
        {
            ReaderId = readerStatus.ReaderId,
            Connected = string.Equals(readerStatus.ReaderStatusValue, "connected", StringComparison.OrdinalIgnoreCase),
            ReaderStatus = readerStatus.ReaderStatusValue,
            Antennas = readerStatus.AntennaList
                .OrderBy(a => a.Port)
                .Select(ToDto)
                .ToArray(),
            DesiredAntennas = desiredAntennas ?? [],
            ReceivedAtUtc = readerStatus.LastPing
        };

    private static double NormalizeSensitivity(double? sensitivity)
    {
        if (!sensitivity.HasValue || sensitivity.Value == 0)
            return DefaultSensitivity;

        var normalized = sensitivity.Value > 0 ? -sensitivity.Value : sensitivity.Value;
        return Math.Clamp(normalized, MinSensitivity, MaxSensitivity);
    }
}
