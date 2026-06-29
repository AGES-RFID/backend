using Backend.Database;
using Backend.Features.Dashboard;
using Backend.Features.GatewayStatus;
using Backend.Features.SystemConfig;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using NSubstitute;

namespace Backend.Tests.Unit.Features.SystemConfigTests;

public class SystemServiceTests
{
    [Fact]
    public async Task GetSystemAsync_ReturnsAggregatedValues()
    {
        var dashboardService = Substitute.For<IDashboardService>();

        dashboardService.GetOccupancyAsync().Returns(new OccupancyDto
        {
            CurrentOccupancy = 5,
            MaxOccupancy = 120,
            OccupancyPercentage = 5.0,
            Vehicles = new List<Backend.Features.Vehicles.VehicleDto>()
        });

        var configuration = new Microsoft.Extensions.Configuration.ConfigurationBuilder().Build();
        await using var db = CreateDbContext();
        var service = new SystemService(dashboardService, configuration, db);

        var result = await service.GetSystemAsync();

        Assert.Equal(120, result.OccupancyLimit);
        Assert.Equal(5, result.CurrentOccupancy);
        Assert.Empty(result.Antennas);
    }

    [Fact]
    public async Task GetSystemAsync_WithAntennaConfiguration_ReturnsParsedAntennas()
    {
        var dashboardService = Substitute.For<IDashboardService>();

        dashboardService.GetOccupancyAsync().Returns(new OccupancyDto
        {
            CurrentOccupancy = 3,
            MaxOccupancy = 100,
            OccupancyPercentage = 3.0,
            Vehicles = new List<Backend.Features.Vehicles.VehicleDto>()
        });

        var inMemorySettings = new Dictionary<string, string?>
        {
            {"Antennas:0:Id", "00000000-0000-0000-0000-000000000001"},
            {"Antennas:0:Number", "1"},
            {"Antennas:0:Status", "On"},
            {"Antennas:0:Sensibility", "80"},
            {"Antennas:0:Power", "30"}
        };

        var configuration = new Microsoft.Extensions.Configuration.ConfigurationBuilder()
            .AddInMemoryCollection(inMemorySettings)
            .Build();

        await using var db = CreateDbContext();
        var service = new SystemService(dashboardService, configuration, db);

        var result = await service.GetSystemAsync();

        Assert.Single(result.Antennas);
        var antenna = result.Antennas[0];
        Assert.Equal(Guid.Parse("00000000-0000-0000-0000-000000000001"), antenna.Id);
        Assert.Equal("Antena 1", antenna.Name);
        Assert.Equal(1, antenna.Number);
        Assert.Equal("On", antenna.Status);
        Assert.Equal(-80, antenna.Sensibility);
        Assert.Equal(30, antenna.Power);
    }

    [Fact]
    public async Task GetAntennasAsync_DoesNotFetchOccupancy()
    {
        var dashboardService = Substitute.For<IDashboardService>();
        var inMemorySettings = new Dictionary<string, string?>
        {
            {"Antennas:0:Id", "00000000-0000-0000-0000-000000000001"},
            {"Antennas:0:Number", "1"},
            {"Antennas:0:Status", "On"},
            {"Antennas:0:Sensibility", "80"},
            {"Antennas:0:Power", "30"}
        };

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(inMemorySettings)
            .Build();

        await using var db = CreateDbContext();
        var service = new SystemService(dashboardService, configuration, db);

        var result = await service.GetAntennasAsync();

        Assert.Single(result);
        await dashboardService.DidNotReceive().GetOccupancyAsync();
    }

    [Fact]
    public async Task GetAntennasAsync_WhenReaderStatusExists_ReturnsAntennasFromDatabase()
    {
        await using var db = CreateDbContext();
        db.ReaderStatuses.Add(new ReaderStatus
        {
            ReaderId = Guid.NewGuid(),
            ReaderStatusValue = "connected",
            LastPing = DateTime.UtcNow,
            AntennaList =
            [
                new ReaderAntennaStatus
                {
                    Port = 2,
                    Power = 30.5,
                    Sensitivity = -70,
                    AntennaStatus = "connected"
                }
            ]
        });
        await db.SaveChangesAsync();
        var service = new SystemService(
            Substitute.For<IDashboardService>(),
            new ConfigurationBuilder().Build(),
            db);

        var result = await service.GetAntennasAsync();

        var antenna = Assert.Single(result);
        Assert.Equal(Guid.Parse("00000000-0000-0000-0000-000000000002"), antenna.Id);
        Assert.Equal(2, antenna.Number);
        Assert.Equal("On", antenna.Status);
        Assert.Equal(-70, antenna.Sensibility);
        Assert.Equal(30.5, antenna.Power);
    }

    [Fact]
    public async Task GetAntennasAsync_WhenReaderStatusHasEmptyAntennaList_InitializesFromConfiguration()
    {
        await using var db = CreateDbContext();
        db.ReaderStatuses.Add(new ReaderStatus
        {
            ReaderId = Guid.NewGuid(),
            ReaderStatusValue = "disconnected",
            LastPing = DateTime.UtcNow,
            AntennaList = []
        });
        await db.SaveChangesAsync();
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                {"Antennas:0:Number", "1"},
                {"Antennas:0:Status", "On"},
                {"Antennas:0:Sensibility", "-65"},
                {"Antennas:0:Power", "25"}
            })
            .Build();
        var service = new SystemService(
            Substitute.For<IDashboardService>(),
            configuration,
            db);

        var result = await service.GetAntennasAsync();

        var antenna = Assert.Single(result);
        Assert.Equal(1, antenna.Number);
        Assert.Equal("On", antenna.Status);
        Assert.Equal(-65, antenna.Sensibility);
        Assert.Equal(25, antenna.Power);

        var saved = await db.ReaderStatuses.SingleAsync();
        var savedAntenna = Assert.Single(saved.AntennaList);
        Assert.Equal(1, savedAntenna.Port);
        Assert.Equal("connected", savedAntenna.AntennaStatus);
        Assert.Equal(-65, savedAntenna.Sensitivity);
        Assert.Equal(25, savedAntenna.Power);
    }

    [Fact]
    public async Task UpdateAntennaAsync_UpdatesSavedAntennaConfigurationForGatewayPolling()
    {
        await using var db = CreateDbContext();
        db.ReaderStatuses.Add(new ReaderStatus
        {
            ReaderId = Guid.NewGuid(),
            ReaderStatusValue = "connected",
            LastPing = DateTime.UtcNow,
            AntennaList =
            [
                new ReaderAntennaStatus
                {
                    Port = 1,
                    Power = 20,
                    Sensitivity = -60,
                    AntennaStatus = "connected"
                }
            ]
        });
        await db.SaveChangesAsync();
        var service = new SystemService(
            Substitute.For<IDashboardService>(),
            new ConfigurationBuilder().Build(),
            db);

        var result = await service.UpdateAntennaAsync(
            Guid.Parse("00000000-0000-0000-0000-000000000001"),
            new UpdateAntennaDto
            {
                Status = "Off",
                Sensibility = -70,
                Power = 30.5
            });

        Assert.Equal("Off", result.Status);
        Assert.Equal(-70, result.Sensibility);
        Assert.Equal(30.5, result.Power);

        var saved = await db.ReaderStatuses.SingleAsync();
        var antenna = Assert.Single(saved.AntennaList);
        Assert.Equal("disconnected", antenna.AntennaStatus);
        Assert.Equal(-70, antenna.Sensitivity);
        Assert.Equal(30.5, antenna.Power);
    }

    private static AppDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new AppDbContext(options);
    }
}
