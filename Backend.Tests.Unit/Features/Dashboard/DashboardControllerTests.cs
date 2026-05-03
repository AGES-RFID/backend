using Backend.Features.Dashboard;
using Microsoft.AspNetCore.Mvc;
using NSubstitute;
using NSubstitute.ExceptionExtensions;

namespace Backend.Tests.Unit.Features.Dashboard;

public class DashboardControllerTests
{
    private static OccupancyDto MakeOccupancyDto(int count = 0) => new()
    {
        CurrentOccupancy = count,
        Vehicles = []
    };

    [Fact]
    public async Task GetOccupancy_WhenServiceSucceeds_ReturnsOk()
    {
        var expected = MakeOccupancyDto(2);

        var service = Substitute.For<IDashboardService>();
        service.GetOccupancyAsync().Returns(expected);

        var controller = new DashboardController(service);

        var result = await controller.GetOccupancy();

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var dto = Assert.IsType<OccupancyDto>(ok.Value);
        Assert.Equal(2, dto.CurrentOccupancy);
    }

    [Fact]
    public async Task GetOccupancy_WhenServiceSucceeds_ReturnsStatusCode200()
    {
        var service = Substitute.For<IDashboardService>();
        service.GetOccupancyAsync().Returns(MakeOccupancyDto());

        var controller = new DashboardController(service);

        var result = await controller.GetOccupancy();

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        Assert.Equal(200, ok.StatusCode);
    }

    [Fact]
    public async Task GetOccupancy_WhenServiceSucceeds_CallsServiceExactlyOnce()
    {
        var service = Substitute.For<IDashboardService>();
        service.GetOccupancyAsync().Returns(MakeOccupancyDto());

        var controller = new DashboardController(service);

        await controller.GetOccupancy();

        await service.Received(1).GetOccupancyAsync();
    }

    [Fact]
    public async Task GetOccupancy_WhenServiceThrowsException_Returns500()
    {
        var service = Substitute.For<IDashboardService>();
        service.GetOccupancyAsync()
            .Returns(Task.FromException<OccupancyDto>(new Exception("db error")));

        var controller = new DashboardController(service);

        var result = await controller.GetOccupancy();

        var statusResult = Assert.IsType<ObjectResult>(result.Result);
        Assert.Equal(500, statusResult.StatusCode);
    }

    [Fact]
    public async Task GetOccupancy_WhenNoVehiclesInside_ReturnsZeroOccupancy()
    {
        var expected = MakeOccupancyDto(0);

        var service = Substitute.For<IDashboardService>();
        service.GetOccupancyAsync().Returns(expected);

        var controller = new DashboardController(service);

        var result = await controller.GetOccupancy();

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var dto = Assert.IsType<OccupancyDto>(ok.Value);
        Assert.Equal(0, dto.CurrentOccupancy);
        Assert.Empty(dto.Vehicles);
    }

    [Fact]
    public async Task GetMetrics_WhenServiceSucceeds_ReturnsOkWithMetrics()
    {
        var expected = new DashboardMetricsDto
        {
            EntriesLastHour = 5,
            ExitsLastHour = 3,
            PeakEntryTime = "14:00"
        };

        var service = Substitute.For<IDashboardService>();
        service.GetMetricsAsync().Returns(expected);

        var controller = new DashboardController(service);
        var result = await controller.GetMetrics();

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var dto = Assert.IsType<DashboardMetricsDto>(ok.Value);
        Assert.Equal(5, dto.EntriesLastHour);
        Assert.Equal(3, dto.ExitsLastHour);
        Assert.Equal("14:00", dto.PeakEntryTime);
    }

    [Fact]
    public async Task GetMetrics_WhenServiceThrows_Returns500()
    {
        var service = Substitute.For<IDashboardService>();
        service.GetMetricsAsync().ThrowsAsync(new Exception("db error"));

        var controller = new DashboardController(service);
        var result = await controller.GetMetrics();

        var status = Assert.IsType<ObjectResult>(result.Result);
        Assert.Equal(500, status.StatusCode);
    }

    [Fact]
    public async Task GetMetrics_WhenNoAccesses_ReturnsZerosAndNullPeakTime()
    {
        var expected = new DashboardMetricsDto
        {
            EntriesLastHour = 0,
            ExitsLastHour = 0,
            PeakEntryTime = null
        };

        var service = Substitute.For<IDashboardService>();
        service.GetMetricsAsync().Returns(expected);

        var controller = new DashboardController(service);
        var result = await controller.GetMetrics();

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var dto = Assert.IsType<DashboardMetricsDto>(ok.Value);
        Assert.Equal(0, dto.EntriesLastHour);
        Assert.Equal(0, dto.ExitsLastHour);
        Assert.Null(dto.PeakEntryTime);
    }
}
