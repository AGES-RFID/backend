using Backend.Features.Settings;
using Backend.Features.SystemConfig;
using Microsoft.AspNetCore.Mvc;
using NSubstitute;
using NSubstitute.ExceptionExtensions;

namespace Backend.Tests.Unit.Features.SystemConfigTests;

public class SystemControllerTests
{
    [Fact]
    public async Task GetMaxOccupancy_WhenSettingExists_ReturnsOkWithValue()
    {
        var settingsService = Substitute.For<ISettingsService>();
        settingsService.GetAsync("max_occupancy", 100).Returns(120);
        var controller = new SystemController(settingsService);

        var result = await controller.GetMaxOccupancy();

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var dto = Assert.IsType<MaxOccupancyDto>(ok.Value);
        Assert.Equal(120, dto.MaxOccupancy);
    }

    [Fact]
    public async Task GetMaxOccupancy_WhenSettingDoesNotExist_ReturnsDefaultValue()
    {
        var settingsService = Substitute.For<ISettingsService>();
        settingsService.GetAsync("max_occupancy", 100).Returns(100);
        var controller = new SystemController(settingsService);

        var result = await controller.GetMaxOccupancy();

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var dto = Assert.IsType<MaxOccupancyDto>(ok.Value);
        Assert.Equal(100, dto.MaxOccupancy);
    }

    [Fact]
    public async Task GetMaxOccupancy_ReturnsStatusCode200()
    {
        var settingsService = Substitute.For<ISettingsService>();
        settingsService.GetAsync("max_occupancy", 100).Returns(100);
        var controller = new SystemController(settingsService);

        var result = await controller.GetMaxOccupancy();

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        Assert.Equal(200, ok.StatusCode);
    }

    [Fact]
    public async Task GetMaxOccupancy_CallsServiceExactlyOnce()
    {
        var settingsService = Substitute.For<ISettingsService>();
        settingsService.GetAsync("max_occupancy", 100).Returns(100);
        var controller = new SystemController(settingsService);

        await controller.GetMaxOccupancy();

        await settingsService.Received(1).GetAsync("max_occupancy", 100);
    }

    [Fact]
    public async Task GetMaxOccupancy_WhenServiceThrows_Returns500()
    {
        var settingsService = Substitute.For<ISettingsService>();
        settingsService.GetAsync("max_occupancy", 100).ThrowsAsync(new Exception("db error"));
        var controller = new SystemController(settingsService);

        var result = await controller.GetMaxOccupancy();

        Assert.IsType<ObjectResult>(result.Result);
        var obj = (ObjectResult)result.Result!;
        Assert.Equal(500, obj.StatusCode);
    }

    [Fact]
    public async Task UpdateMaxOccupancy_WhenValidInput_ReturnsOkWithUpdatedValue()
    {
        var settingsService = Substitute.For<ISettingsService>();
        settingsService.SetAsync("max_occupancy", "150").Returns("150");
        var controller = new SystemController(settingsService);

        var result = await controller.UpdateMaxOccupancy(new UpdateMaxOccupancyDto { MaxOccupancy = 150 });

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var dto = Assert.IsType<MaxOccupancyDto>(ok.Value);
        Assert.Equal(150, dto.MaxOccupancy);
    }

    [Fact]
    public async Task UpdateMaxOccupancy_CallsSetAsyncWithCorrectArguments()
    {
        var settingsService = Substitute.For<ISettingsService>();
        settingsService.SetAsync("max_occupancy", "200").Returns("200");
        var controller = new SystemController(settingsService);

        await controller.UpdateMaxOccupancy(new UpdateMaxOccupancyDto { MaxOccupancy = 200 });

        await settingsService.Received(1).SetAsync("max_occupancy", "200");
    }

    [Fact]
    public async Task UpdateMaxOccupancy_WhenServiceThrows_Returns500()
    {
        var settingsService = Substitute.For<ISettingsService>();
        settingsService.SetAsync(Arg.Any<string>(), Arg.Any<string>()).ThrowsAsync(new Exception("db error"));
        var controller = new SystemController(settingsService);

        var result = await controller.UpdateMaxOccupancy(new UpdateMaxOccupancyDto { MaxOccupancy = 100 });

        Assert.IsType<ObjectResult>(result.Result);
        var obj = (ObjectResult)result.Result!;
        Assert.Equal(500, obj.StatusCode);
    }
}
