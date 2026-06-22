using Backend.Features.Dashboard;
using Microsoft.AspNetCore.Mvc;
using NSubstitute;
using NSubstitute.ExceptionExtensions;

namespace Backend.Tests.Unit.Features.Dashboard;

public class DashboardControllerTests
{
    private static OccupancyDto MakeOccupancyDto(int count = 0, int maxOccupancy = 100, double percentage = 0) => new()
    {
        CurrentOccupancy = count,
        MaxOccupancy = maxOccupancy,
        OccupancyPercentage = percentage,
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
            PeakHourEntries = 5,
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
            PeakEntryTime = null,
            PeakHourEntries = 0
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

    [Fact]
    public async Task GetMetrics_WhenServiceReturnsMaxOccupancy_ReturnsCorrectValue()
    {
        var expected = new DashboardMetricsDto
        {
            EntriesLastHour = 0,
            ExitsLastHour = 0,
            PeakEntryTime = null,
            PeakHourEntries = 0,
            MaxOccupancy = 150
        };

        var service = Substitute.For<IDashboardService>();
        service.GetMetricsAsync().Returns(expected);

        var controller = new DashboardController(service);
        var result = await controller.GetMetrics();

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var dto = Assert.IsType<DashboardMetricsDto>(ok.Value);
        Assert.Equal(150, dto.MaxOccupancy);
    }

    [Fact]
    public async Task GetMetrics_WhenServiceReturnsPeakOccupancy_ReturnsCorrectValue()
    {
        var expected = new DashboardMetricsDto
        {
            PeakHourEntries = 42,
            PeakEntryTime = "14:00"
        };
        var service = Substitute.For<IDashboardService>();
        service.GetMetricsAsync().Returns(expected);
        var controller = new DashboardController(service);

        var result = await controller.GetMetrics();

        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var dto = Assert.IsType<DashboardMetricsDto>(okResult.Value);
        Assert.Equal(42, dto.PeakHourEntries);
        Assert.Equal("14:00", dto.PeakEntryTime);
    }
    [Fact]
    public async Task GetDashboard_WhenServiceSucceeds_ReturnsOkWithDashboardMetrics()
    {
        var expected = new DashboardMetricsDto
        {
            EntriesLastHour = 10,
            PeakHourEntries = 5,
            PeakEntryTime = "10:00",
            UpdatedAt = DateTime.UtcNow
        };

        var service = Substitute.For<IDashboardService>();
        service.GetDashboardAsync().Returns(expected);

        var controller = new DashboardController(service);
        var result = await controller.GetDashboard();

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var dto = Assert.IsType<DashboardMetricsDto>(ok.Value);
        Assert.Equal(10, dto.EntriesLastHour);
        Assert.Equal(5, dto.PeakHourEntries);
        Assert.Equal("10:00", dto.PeakEntryTime);
        Assert.Equal(expected.UpdatedAt, dto.UpdatedAt);
    }

    [Fact]
    public async Task GetDashboard_WhenServiceThrows_Returns500()
    {
        var service = Substitute.For<IDashboardService>();
        service.GetDashboardAsync().ThrowsAsync(new Exception("db error"));

        var controller = new DashboardController(service);
        var result = await controller.GetDashboard();

        var status = Assert.IsType<ObjectResult>(result.Result);
        Assert.Equal(500, status.StatusCode);
    }

    [Fact]
    public async Task GetPermanence_WhenServiceSucceeds_ReturnsOk()
    {
        var expected = new List<PermanenceDto>
        {
            new() { RfidTag = "EPC-001", Plate = "ABC-1234", MinutesParked = 30 }
        };

        var service = Substitute.For<IDashboardService>();
        service.GetPermanenceAsync().Returns(expected);

        var controller = new DashboardController(service);

        var result = await controller.GetPermanence();

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var dto = Assert.IsType<List<PermanenceDto>>(ok.Value);
        Assert.Single(dto);
        Assert.Equal("EPC-001", dto[0].RfidTag);
        Assert.Equal("ABC-1234", dto[0].Plate);
        Assert.Equal(30, dto[0].MinutesParked);
    }

    [Fact]
    public async Task GetPermanence_WhenServiceSucceeds_ReturnsStatusCode200()
    {
        var service = Substitute.For<IDashboardService>();
        service.GetPermanenceAsync().Returns(new List<PermanenceDto>());

        var controller = new DashboardController(service);

        var result = await controller.GetPermanence();

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        Assert.Equal(200, ok.StatusCode);
    }

    [Fact]
    public async Task GetPermanence_WhenServiceThrows_Returns500()
    {
        var service = Substitute.For<IDashboardService>();
        service.GetPermanenceAsync().ThrowsAsync(new Exception("db error"));

        var controller = new DashboardController(service);

        var result = await controller.GetPermanence();

        var status = Assert.IsType<ObjectResult>(result.Result);
        Assert.Equal(500, status.StatusCode);
    }

    [Fact]
    public async Task GetPermanence_WhenNoVehiclesInside_ReturnsEmptyList()
    {
        var service = Substitute.For<IDashboardService>();
        service.GetPermanenceAsync().Returns(new List<PermanenceDto>());

        var controller = new DashboardController(service);

        var result = await controller.GetPermanence();

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var dto = Assert.IsType<List<PermanenceDto>>(ok.Value);
        Assert.Empty(dto);
    }

}