using Backend.Features.Dashboard;
using Microsoft.AspNetCore.Mvc;
using NSubstitute;
using NSubstitute.ExceptionExtensions;

namespace Backend.Tests.Unit.Features.Dashboard;

public class DashboardControllerTests
{
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