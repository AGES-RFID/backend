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

    [Fact]
    public async Task GetSystem_WhenSuccessful_ReturnsOkWithSystemDto()
    {
        var settingsService = Substitute.For<ISettingsService>();
        var systemService = Substitute.For<ISystemService>();
        var expectedSystem = new SystemDto
        {
            OccupancyLimit = 120,
            CurrentOccupancy = 10,
            Antennas = new List<AntennaDto>()
        };
        systemService.GetSystemAsync().Returns(expectedSystem);
        var controller = new SystemController(settingsService);

        var result = await controller.GetSystem(systemService);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var dto = Assert.IsType<SystemDto>(ok.Value);
        Assert.Equal(120, dto.OccupancyLimit);
        Assert.Equal(10, dto.CurrentOccupancy);
    }

    [Fact]
    public async Task GetSystem_WhenServiceThrows_Returns500()
    {
        var settingsService = Substitute.For<ISettingsService>();
        var systemService = Substitute.For<ISystemService>();
        systemService.GetSystemAsync().ThrowsAsync(new Exception("service error"));
        var controller = new SystemController(settingsService);

        var result = await controller.GetSystem(systemService);

        Assert.IsType<ObjectResult>(result.Result);
        var obj = (ObjectResult)result.Result!;
        Assert.Equal(500, obj.StatusCode);
    }

    [Fact]
    public async Task GetAntennas_WhenSuccessful_ReturnsOkWithAntennas()
    {
        var settingsService = Substitute.For<ISettingsService>();
        var systemService = Substitute.For<ISystemService>();
        var expectedAntennas = new List<AntennaDto>
        {
            new AntennaDto { Id = Guid.NewGuid(), Name = "Antena 1", Number = 1, Status = "On" }
        };
        systemService.GetAntennasAsync().Returns(expectedAntennas);
        var controller = new SystemController(settingsService);

        var result = await controller.GetAntennas(systemService);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var list = Assert.IsType<List<AntennaDto>>(ok.Value);
        Assert.Single(list);
        Assert.Equal("Antena 1", list[0].Name);
    }

    [Fact]
    public async Task GetAntennas_WhenServiceThrows_Returns500()
    {
        var settingsService = Substitute.For<ISettingsService>();
        var systemService = Substitute.For<ISystemService>();
        systemService.GetAntennasAsync().ThrowsAsync(new Exception("service error"));
        var controller = new SystemController(settingsService);

        var result = await controller.GetAntennas(systemService);

        Assert.IsType<ObjectResult>(result.Result);
        var obj = (ObjectResult)result.Result!;
        Assert.Equal(500, obj.StatusCode);
    }

    [Fact]
    public async Task UpdateAntenna_WhenSuccessful_ReturnsOkWithUpdatedAntenna()
    {
        var settingsService = Substitute.For<ISettingsService>();
        var systemService = Substitute.For<ISystemService>();
        var antennaId = Guid.Parse("00000000-0000-0000-0000-000000000001");
        var updateDto = new UpdateAntennaDto
        {
            Status = "Off",
            Sensibility = -70,
            Power = 30.5
        };
        var expected = new AntennaDto
        {
            Id = antennaId,
            Name = "Antena 1",
            Number = 1,
            Status = "Off",
            Sensibility = -70,
            Power = 30.5
        };
        systemService.UpdateAntennaAsync(antennaId, updateDto).Returns(expected);
        var controller = new SystemController(settingsService);

        var result = await controller.UpdateAntenna(antennaId, updateDto, systemService);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var antenna = Assert.IsType<AntennaDto>(ok.Value);
        Assert.Equal("Off", antenna.Status);
        Assert.Equal(30.5, antenna.Power);
        await systemService.Received(1).UpdateAntennaAsync(antennaId, updateDto);
    }

    [Fact]
    public async Task UpdateAntenna_WhenNotFound_ReturnsNotFound()
    {
        var settingsService = Substitute.For<ISettingsService>();
        var systemService = Substitute.For<ISystemService>();
        var antennaId = Guid.NewGuid();
        var updateDto = new UpdateAntennaDto { Status = "On" };
        systemService.UpdateAntennaAsync(antennaId, updateDto)
            .ThrowsAsync(new KeyNotFoundException());
        var controller = new SystemController(settingsService);

        var result = await controller.UpdateAntenna(antennaId, updateDto, systemService);

        Assert.IsType<NotFoundResult>(result.Result);
    }
}
