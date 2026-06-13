using Backend.Features.Dashboard;
using Backend.Features.SystemConfig;
using Backend.Features.Settings;
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
}
