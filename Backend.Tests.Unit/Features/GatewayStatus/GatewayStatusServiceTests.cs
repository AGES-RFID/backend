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

    [Fact]
    public async Task SaveStatusAsync_WhenCurrentAntennaDiffersFromSaved_ReturnsDesiredAntennasWithoutOverwritingSavedConfiguration()
    {
        await using var db = CreateDbContext();
        var service = new GatewayStatusService(db);
        var readerId = Guid.NewGuid();

        await service.SaveStatusAsync(new ReaderStatusDto
        {
            ReaderId = readerId,
            Connected = true,
            Antennas = [new AntennaStatusDto { Port = 1, Connected = true, Power = 30, Sensitivity = -70 }]
        });

        var response = await service.SaveStatusAsync(new ReaderStatusDto
        {
            ReaderId = readerId,
            Connected = true,
            Antennas = [new AntennaStatusDto { Port = 1, Connected = true, Power = 20, Sensitivity = -60 }]
        });

        var desired = Assert.Single(response.DesiredAntennas);
        Assert.Equal((ushort)1, desired.Port);
        Assert.Equal(30, desired.Power);
        Assert.Equal(-70, desired.Sensitivity);

        var persisted = await db.ReaderStatuses.SingleAsync(r => r.ReaderId == readerId);
        Assert.Equal(30, persisted.AntennaList.Single().Power);
        Assert.Equal(-70, persisted.AntennaList.Single().Sensitivity);
    }

    [Fact]
    public async Task SaveStatusAsync_WhenStatusHasNoAntennas_DoesNotClearSavedAntennaList()
    {
        await using var db = CreateDbContext();
        var service = new GatewayStatusService(db);
        var readerId = Guid.NewGuid();

        await service.SaveStatusAsync(new ReaderStatusDto
        {
            ReaderId = readerId,
            Connected = true,
            Antennas = [new AntennaStatusDto { Port = 1, Connected = true, Power = 30, Sensitivity = -70 }]
        });

        await service.SaveStatusAsync(new ReaderStatusDto
        {
            ReaderId = readerId,
            Connected = false,
            Antennas = []
        });

        var persisted = await db.ReaderStatuses.SingleAsync(r => r.ReaderId == readerId);
        var antenna = Assert.Single(persisted.AntennaList);
        Assert.Equal(1, antenna.Port);
        Assert.Equal(30, antenna.Power);
        Assert.Equal(-70, antenna.Sensitivity);
        Assert.Equal("connected", antenna.AntennaStatus);
    }

    [Fact]
    public async Task ConfirmConfigurationAsync_UpdatesSavedAntennaConfiguration()
    {
        await using var db = CreateDbContext();
        var service = new GatewayStatusService(db);
        var readerId = Guid.NewGuid();

        await service.SaveStatusAsync(new ReaderStatusDto
        {
            ReaderId = readerId,
            Connected = true,
            Antennas = [new AntennaStatusDto { Port = 1, Connected = true, Power = 30, Sensitivity = -70 }]
        });

        var response = await service.ConfirmConfigurationAsync(new ReaderStatusDto
        {
            ReaderId = readerId,
            Connected = true,
            Antennas = [new AntennaStatusDto { Port = 1, Connected = true, Power = 25, Sensitivity = -65 }]
        });

        Assert.Empty(response.DesiredAntennas);
        var persisted = await db.ReaderStatuses.SingleAsync(r => r.ReaderId == readerId);
        Assert.Equal(25, persisted.AntennaList.Single().Power);
        Assert.Equal(-65, persisted.AntennaList.Single().Sensitivity);
    }

    [Fact]
    public async Task ConfirmConfigurationAsync_WhenConfirmationOmitsAntenna_KeepsSavedAntennaConfiguration()
    {
        await using var db = CreateDbContext();
        var service = new GatewayStatusService(db);
        var readerId = Guid.NewGuid();

        await service.SaveStatusAsync(new ReaderStatusDto
        {
            ReaderId = readerId,
            Connected = true,
            Antennas =
            [
                new AntennaStatusDto { Port = 1, Connected = false, Power = 20, Sensitivity = -70 },
                new AntennaStatusDto { Port = 2, Connected = true, Power = 25, Sensitivity = -65 }
            ]
        });

        await service.ConfirmConfigurationAsync(new ReaderStatusDto
        {
            ReaderId = readerId,
            Connected = true,
            Antennas = [new AntennaStatusDto { Port = 2, Connected = true, Power = 30, Sensitivity = -60 }]
        });

        var persisted = await db.ReaderStatuses.SingleAsync(r => r.ReaderId == readerId);
        Assert.Collection(
            persisted.AntennaList.OrderBy(a => a.Port),
            antenna =>
            {
                Assert.Equal(1, antenna.Port);
                Assert.Equal("disconnected", antenna.AntennaStatus);
                Assert.Equal(20, antenna.Power);
                Assert.Equal(-70, antenna.Sensitivity);
            },
            antenna =>
            {
                Assert.Equal(2, antenna.Port);
                Assert.Equal("connected", antenna.AntennaStatus);
                Assert.Equal(30, antenna.Power);
                Assert.Equal(-60, antenna.Sensitivity);
            });
    }

    private static AppDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new AppDbContext(options);
    }
}
