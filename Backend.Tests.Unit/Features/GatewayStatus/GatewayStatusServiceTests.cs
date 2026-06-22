using Backend.Database;
using Backend.Features.GatewayStatus;
using Microsoft.EntityFrameworkCore;

namespace Backend.Tests.Unit.Features.GatewayStatus;

public class GatewayStatusServiceTests
{
    [Fact]
    public async Task SaveStatusAsync_StoresStatusInDatabaseOrderedByAntennaPort()
    {
        await using var db = CreateDbContext();
        var service = new GatewayStatusService(db);
        var readerId = Guid.NewGuid();
        var status = new ReaderStatusDto
        {
            ReaderId = readerId,
            Connected = true,
            Antennas =
            [
                new AntennaStatusDto { Port = 2, Connected = true, Power = 31.5, Sensitivity = -71 },
                new AntennaStatusDto { Port = 1, Connected = false, Power = 30, Sensitivity = -70 }
            ]
        };

        var saved = await service.SaveStatusAsync(status);
        var lastStatus = await service.GetLastStatusAsync();
        var persisted = await db.ReaderStatuses.SingleAsync(r => r.ReaderId == readerId);

        Assert.Equal(readerId, saved.ReaderId);
        Assert.Equal(readerId, lastStatus?.ReaderId);
        Assert.True(saved.Connected);
        Assert.Equal("connected", saved.ReaderStatus);
        Assert.Equal("connected", persisted.ReaderStatusValue);
        Assert.Collection(
            saved.Antennas,
            antenna => Assert.Equal((ushort)1, antenna.Port),
            antenna => Assert.Equal((ushort)2, antenna.Port));
        Assert.True(saved.ReceivedAtUtc <= DateTime.UtcNow);
    }

    [Fact]
    public async Task SaveStatusAsync_WhenReaderAlreadyExists_UpdatesSameReaderStatus()
    {
        await using var db = CreateDbContext();
        var service = new GatewayStatusService(db);
        var readerId = Guid.NewGuid();

        await service.SaveStatusAsync(new ReaderStatusDto { ReaderId = readerId, Connected = true });
        var updated = await service.SaveStatusAsync(new ReaderStatusDto { ReaderId = readerId, Connected = false });

        Assert.Equal("disconnected", updated.ReaderStatus);
        Assert.Equal(1, await db.ReaderStatuses.CountAsync());
    }

    private static AppDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new AppDbContext(options);
    }
}
