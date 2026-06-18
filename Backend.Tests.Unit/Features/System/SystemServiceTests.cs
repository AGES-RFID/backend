using Backend.Features.Dashboard;
using Backend.Features.SystemConfig;
using Backend.Features.Settings;
using Microsoft.Extensions.Configuration;
using NSubstitute;

namespace Backend.Tests.Unit.Features.SystemConfigTests;

public class SystemServiceTests
{
    [Fact]
    public async Task GetSystemAsync_ReturnsAggregatedValues()
    {
        var dashboardService = Substitute.For<IDashboardService>();
        var settingsService = Substitute.For<ISettingsService>();

        dashboardService.GetOccupancyAsync().Returns(new OccupancyDto
        {
            CurrentOccupancy = 5,
            MaxOccupancy = 100,
            OccupancyPercentage = 5.0,
            Vehicles = new List<Backend.Features.Vehicles.VehicleDto>()
        });

        settingsService.GetAsync("max_occupancy", 100).Returns(120);

        var configuration = new Microsoft.Extensions.Configuration.ConfigurationBuilder().Build();
        var service = new SystemService(dashboardService, settingsService, configuration);

        var result = await service.GetSystemAsync();

        Assert.Equal(120, result.OccupancyLimit);
        Assert.Equal(5, result.CurrentOccupancy);
        Assert.Empty(result.Antennas);
    }

    [Fact]
    public async Task GetSystemAsync_WithAntennaConfiguration_ReturnsParsedAntennas()
    {
        var dashboardService = Substitute.For<IDashboardService>();
        var settingsService = Substitute.For<ISettingsService>();

        dashboardService.GetOccupancyAsync().Returns(new OccupancyDto
        {
            CurrentOccupancy = 3,
            MaxOccupancy = 100,
            OccupancyPercentage = 3.0,
            Vehicles = new List<Backend.Features.Vehicles.VehicleDto>()
        });

        settingsService.GetAsync("max_occupancy", 100).Returns(100);

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

        var service = new SystemService(dashboardService, settingsService, configuration);

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
}
