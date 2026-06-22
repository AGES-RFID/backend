using Backend.Database;
using Microsoft.EntityFrameworkCore;

namespace Backend.Features.GatewayStatus;

public sealed class GatewayStatusService(AppDbContext db) : IGatewayStatusService
{
    private static readonly Guid DefaultReaderId = Guid.Parse("00000000-0000-0000-0000-000000000001");
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
            readerStatus = new ReaderStatus { ReaderId = readerId };
            await _db.ReaderStatuses.AddAsync(readerStatus);
        }

        readerStatus.ReaderStatusValue = readerStatusValue;
        readerStatus.LastPing = lastPing;
        readerStatus.AntennaList = status.Antennas
            .OrderBy(a => a.Port)
            .Select(a => new ReaderAntennaStatus
            {
                Port = a.Port,
                Power = a.Power,
                Sensitivity = a.Sensitivity,
                AntennaStatus = a.AntennaStatus
            })
            .ToList();

        await _db.SaveChangesAsync();

        return ToResponse(readerStatus);
    }

    public async Task<ReaderStatusResponseDto?> GetLastStatusAsync()
    {
        var readerStatus = await _db.ReaderStatuses
            .AsNoTracking()
            .OrderByDescending(r => r.LastPing)
            .FirstOrDefaultAsync();

        return readerStatus is null ? null : ToResponse(readerStatus);
    }

    private static ReaderStatusResponseDto ToResponse(ReaderStatus readerStatus) =>
        new()
        {
            ReaderId = readerStatus.ReaderId,
            Connected = string.Equals(readerStatus.ReaderStatusValue, "connected", StringComparison.OrdinalIgnoreCase),
            ReaderStatus = readerStatus.ReaderStatusValue,
            Antennas = readerStatus.AntennaList
                .OrderBy(a => a.Port)
                .Select(a => new AntennaStatusDto
                {
                    Port = (ushort)a.Port,
                    Connected = string.Equals(a.AntennaStatus, "connected", StringComparison.OrdinalIgnoreCase),
                    Power = a.Power,
                    Sensitivity = a.Sensitivity
                })
                .ToArray(),
            ReceivedAtUtc = readerStatus.LastPing
        };
}
