using Backend.Features.Antennas;
using Microsoft.AspNetCore.Mvc;
using NSubstitute;
using NSubstitute.ExceptionExtensions;

namespace Backend.Tests.Unit.Features.Antennas;

public class AntennaControllerTests
{
    // --- GET /api/antennas ---

    [Fact]
    public async Task GetAntennas_WhenServiceReturnsAntennas_ReturnsOkWithList()
    {
        var expected = new List<AntennaDto>
        {
            new() { Id = Guid.NewGuid(), Number = 1, Status = "On", Sensibility = 80, Power = 30 }
        };
        var service = Substitute.For<IAntennaService>();
        service.GetAntennasAsync().Returns(expected);
        var controller = new AntennaController(service);

        var result = await controller.GetAntennas();

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        Assert.Equal(expected, ok.Value);
        await service.Received(1).GetAntennasAsync();
    }

    [Fact]
    public async Task GetAntennas_WhenGatewayThrows_Returns502()
    {
        var service = Substitute.For<IAntennaService>();
        service.GetAntennasAsync().ThrowsAsync(new GatewayException(503));
        var controller = new AntennaController(service);

        var result = await controller.GetAntennas();

        var statusResult = Assert.IsType<ObjectResult>(result.Result);
        Assert.Equal(502, statusResult.StatusCode);
    }

    [Fact]
    public async Task GetAntennas_WhenUnexpectedExceptionThrown_Returns500()
    {
        var service = Substitute.For<IAntennaService>();
        service.GetAntennasAsync().ThrowsAsync(new Exception("boom"));
        var controller = new AntennaController(service);

        var result = await controller.GetAntennas();

        var statusResult = Assert.IsType<ObjectResult>(result.Result);
        Assert.Equal(500, statusResult.StatusCode);
    }

    // --- GET /api/antennas/{id} ---

    [Fact]
    public async Task GetAntenna_WhenServiceReturnsAntenna_ReturnsOk()
    {
        var id = Guid.NewGuid();
        var expected = new AntennaDto { Id = id, Number = 1, Status = "On" };
        var service = Substitute.For<IAntennaService>();
        service.GetAntennaAsync(id).Returns(expected);
        var controller = new AntennaController(service);

        var result = await controller.GetAntenna(id);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        Assert.Equal(expected, ok.Value);
    }

    [Fact]
    public async Task GetAntenna_WhenNotFound_Returns404()
    {
        var id = Guid.NewGuid();
        var service = Substitute.For<IAntennaService>();
        service.GetAntennaAsync(id).ThrowsAsync(new KeyNotFoundException());
        var controller = new AntennaController(service);

        var result = await controller.GetAntenna(id);

        Assert.IsType<NotFoundResult>(result.Result);
    }

    [Fact]
    public async Task GetAntenna_WhenGatewayThrows_Returns502()
    {
        var service = Substitute.For<IAntennaService>();
        service.GetAntennaAsync(Arg.Any<Guid>()).ThrowsAsync(new GatewayException(500));
        var controller = new AntennaController(service);

        var result = await controller.GetAntenna(Guid.NewGuid());

        var statusResult = Assert.IsType<ObjectResult>(result.Result);
        Assert.Equal(502, statusResult.StatusCode);
    }

    [Fact]
    public async Task GetAntenna_WhenUnexpectedExceptionThrown_Returns500()
    {
        var service = Substitute.For<IAntennaService>();
        service.GetAntennaAsync(Arg.Any<Guid>()).ThrowsAsync(new Exception("boom"));
        var controller = new AntennaController(service);

        var result = await controller.GetAntenna(Guid.NewGuid());

        var statusResult = Assert.IsType<ObjectResult>(result.Result);
        Assert.Equal(500, statusResult.StatusCode);
    }

    // --- PUT /api/antennas/{id} ---

    [Fact]
    public async Task UpdateAntenna_WhenServiceReturnsAntenna_ReturnsOk()
    {
        var id = Guid.NewGuid();
        var dto = new UpdateAntennaDto { Status = "On", Sensibility = 80, Power = 30 };
        var expected = new AntennaDto { Id = id, Number = 1, Status = "On", Sensibility = 80, Power = 30 };
        var service = Substitute.For<IAntennaService>();
        service.UpdateAntennaAsync(id, dto).Returns(expected);
        var controller = new AntennaController(service);

        var result = await controller.UpdateAntenna(id, dto);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        Assert.Equal(expected, ok.Value);
    }

    [Fact]
    public async Task UpdateAntenna_WhenValidationFails_Returns400()
    {
        var service = Substitute.For<IAntennaService>();
        service.UpdateAntennaAsync(Arg.Any<Guid>(), Arg.Any<UpdateAntennaDto>())
            .ThrowsAsync(new ArgumentOutOfRangeException("sensibility", "Sensibility must be between 0 and 100"));
        var controller = new AntennaController(service);

        var result = await controller.UpdateAntenna(Guid.NewGuid(), new UpdateAntennaDto { Sensibility = 101 });

        Assert.IsType<BadRequestObjectResult>(result.Result);
    }

    [Fact]
    public async Task UpdateAntenna_WhenNotFound_Returns404()
    {
        var service = Substitute.For<IAntennaService>();
        service.UpdateAntennaAsync(Arg.Any<Guid>(), Arg.Any<UpdateAntennaDto>())
            .ThrowsAsync(new KeyNotFoundException());
        var controller = new AntennaController(service);

        var result = await controller.UpdateAntenna(Guid.NewGuid(), new UpdateAntennaDto());

        Assert.IsType<NotFoundResult>(result.Result);
    }

    [Fact]
    public async Task UpdateAntenna_WhenGatewayThrows_Returns502()
    {
        var service = Substitute.For<IAntennaService>();
        service.UpdateAntennaAsync(Arg.Any<Guid>(), Arg.Any<UpdateAntennaDto>())
            .ThrowsAsync(new GatewayException(503));
        var controller = new AntennaController(service);

        var result = await controller.UpdateAntenna(Guid.NewGuid(), new UpdateAntennaDto());

        var statusResult = Assert.IsType<ObjectResult>(result.Result);
        Assert.Equal(502, statusResult.StatusCode);
    }

    [Fact]
    public async Task UpdateAntenna_WhenUnexpectedExceptionThrown_Returns500()
    {
        var service = Substitute.For<IAntennaService>();
        service.UpdateAntennaAsync(Arg.Any<Guid>(), Arg.Any<UpdateAntennaDto>())
            .ThrowsAsync(new Exception("boom"));
        var controller = new AntennaController(service);

        var result = await controller.UpdateAntenna(Guid.NewGuid(), new UpdateAntennaDto());

        var statusResult = Assert.IsType<ObjectResult>(result.Result);
        Assert.Equal(500, statusResult.StatusCode);
    }
}
