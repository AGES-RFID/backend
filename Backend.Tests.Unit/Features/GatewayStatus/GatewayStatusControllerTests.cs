using Backend.Features.GatewayStatus;
using Microsoft.AspNetCore.Mvc;
using NSubstitute;

namespace Backend.Tests.Unit.Features.GatewayStatus;

public class GatewayStatusControllerTests
{
    [Fact]
    public async Task ReceiveStatus_WhenPayloadIsValid_ReturnsOkWithSavedStatus()
    {
        var dto = new ReaderStatusDto
        {
            Connected = true,
            Antennas =
            [
                new AntennaStatusDto
                {
                    Port = 2,
                    Connected = true,
                    Power = 30,
                    Sensitivity = -70
                }
            ]
        };
        var expected = new ReaderStatusResponseDto
        {
            Connected = true,
            Antennas = dto.Antennas,
            ReceivedAtUtc = DateTime.UtcNow
        };

        var service = Substitute.For<IGatewayStatusService>();
        service.SaveStatusAsync(dto).Returns(expected);
        var controller = new GatewayStatusController(service);

        var result = await controller.ReceiveStatus(dto);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var response = Assert.IsType<ReaderStatusResponseDto>(ok.Value);

        Assert.True(response.Connected);
        Assert.Single(response.Antennas);
        await service.Received(1).SaveStatusAsync(dto);
    }

    [Fact]
    public async Task ReceiveStatus_WhenModelStateIsInvalid_ReturnsBadRequest()
    {
        var service = Substitute.For<IGatewayStatusService>();
        var controller = new GatewayStatusController(service);
        controller.ModelState.AddModelError("Connected", "required");

        var result = await controller.ReceiveStatus(new ReaderStatusDto());

        Assert.IsType<BadRequestObjectResult>(result.Result);
        await service.DidNotReceiveWithAnyArgs().SaveStatusAsync(default!);
    }

    [Fact]
    public async Task GetLastStatus_WhenStatusWasReceived_ReturnsOk()
    {
        var expected = new ReaderStatusResponseDto
        {
            Connected = false,
            Antennas = [],
            ReceivedAtUtc = DateTime.UtcNow
        };
        var service = Substitute.For<IGatewayStatusService>();
        service.GetLastStatusAsync().Returns(expected);
        var controller = new GatewayStatusController(service);

        var result = await controller.GetLastStatus();

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var response = Assert.IsType<ReaderStatusResponseDto>(ok.Value);

        Assert.False(response.Connected);
        await service.Received(1).GetLastStatusAsync();
    }

    [Fact]
    public async Task GetLastStatus_WhenStatusWasNotReceived_ReturnsNotFound()
    {
        var service = Substitute.For<IGatewayStatusService>();
        var controller = new GatewayStatusController(service);

        var result = await controller.GetLastStatus();

        Assert.IsType<NotFoundObjectResult>(result.Result);
        await service.Received(1).GetLastStatusAsync();
    }

    [Fact]
    public async Task SyncAntennaConfiguration_WhenPayloadIsValid_ReturnsOkWithSavedStatus()
    {
        var service = Substitute.For<IGatewayStatusService>();
        var dto = new ReaderStatusDto
        {
            Connected = true,
            Antennas = [new AntennaStatusDto { Port = 1, Connected = true, Power = 30, Sensitivity = -70 }]
        };
        var expected = new ReaderStatusResponseDto
        {
            Connected = true,
            Antennas = dto.Antennas,
            ReceivedAtUtc = DateTime.UtcNow
        };
        service.ConfirmConfigurationAsync(dto).Returns(expected);
        var controller = new GatewayStatusController(service);

        var result = await controller.SyncAntennaConfiguration(dto);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        Assert.Equal(expected, ok.Value);
        await service.Received(1).ConfirmConfigurationAsync(dto);
    }

    [Fact]
    public async Task SyncAntennaConfiguration_WhenModelStateIsInvalid_ReturnsBadRequest()
    {
        var service = Substitute.For<IGatewayStatusService>();
        var controller = new GatewayStatusController(service);
        controller.ModelState.AddModelError("Power", "required");

        var result = await controller.SyncAntennaConfiguration(new ReaderStatusDto());

        Assert.IsType<BadRequestObjectResult>(result.Result);
        await service.DidNotReceiveWithAnyArgs().ConfirmConfigurationAsync(default!);
    }
}
