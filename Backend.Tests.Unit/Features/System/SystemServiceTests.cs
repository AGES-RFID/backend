using Backend.Features.Dashboard;
using Backend.Features.SystemConfig;
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
        var service = new SystemService(dashboardService, configuration);

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

        var service = new SystemService(dashboardService, configuration);

        var result = await service.GetSystemAsync();

        Assert.Single(result.Antennas);
        var antenna = result.Antennas[0];
        Assert.Equal(Guid.Parse("00000000-0000-0000-0000-000000000001"), antenna.Id);
        Assert.Equal("Antena 1", antenna.Name);
        Assert.Equal(1, antenna.Number);
        Assert.Equal("On", antenna.Status);
        Assert.Equal(80, antenna.Sensibility);
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

        var service = new SystemService(dashboardService, configuration);

        var result = await service.GetAntennasAsync();

        Assert.Single(result);
        await dashboardService.DidNotReceive().GetOccupancyAsync();
    }
}
