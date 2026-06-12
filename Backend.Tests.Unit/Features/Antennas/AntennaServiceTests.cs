using Backend.Features.Antennas;
using NSubstitute;

namespace Backend.Tests.Unit.Features.Antennas;

public class AntennaServiceTests
{
    // --- GetAntennasAsync ---

    [Fact]
    public async Task GetAntennasAsync_DelegatesToGatewayClient()
    {
        var expected = new List<AntennaDto>
        {
            new() { Id = Guid.NewGuid(), Number = 1, Status = "On", Sensibility = 80, Power = 30 }
        };
        var client = Substitute.For<IGatewayClient>();
        client.GetAntennasAsync().Returns(expected);
        var service = new AntennaService(client);

        var result = await service.GetAntennasAsync();

        Assert.Equal(expected, result);
        await client.Received(1).GetAntennasAsync();
    }

    // --- GetAntennaAsync ---

    [Fact]
    public async Task GetAntennaAsync_DelegatesToGatewayClient()
    {
        var id = Guid.NewGuid();
        var expected = new AntennaDto { Id = id, Number = 1, Status = "On" };
        var client = Substitute.For<IGatewayClient>();
        client.GetAntennaAsync(id).Returns(expected);
        var service = new AntennaService(client);

        var result = await service.GetAntennaAsync(id);

        Assert.Equal(expected, result);
        await client.Received(1).GetAntennaAsync(id);
    }

    // --- UpdateAntennaAsync ---

    [Fact]
    public async Task UpdateAntennaAsync_WhenStatusIsNull_DefaultsToOff()
    {
        var id = Guid.NewGuid();
        var dto = new UpdateAntennaDto { Status = null, Sensibility = 50, Power = 40 };
        var expected = new AntennaDto { Id = id, Number = 1, Status = "Off" };
        var client = Substitute.For<IGatewayClient>();
        client.UpdateAntennaAsync(id, Arg.Is<UpdateAntennaDto>(d => d.Status == "Off")).Returns(expected);
        var service = new AntennaService(client);

        await service.UpdateAntennaAsync(id, dto);

        await client.Received(1).UpdateAntennaAsync(id, Arg.Is<UpdateAntennaDto>(d => d.Status == "Off"));
    }

    [Fact]
    public async Task UpdateAntennaAsync_WhenSensibilityAbove100_ThrowsArgumentOutOfRange()
    {
        var client = Substitute.For<IGatewayClient>();
        var service = new AntennaService(client);

        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() =>
            service.UpdateAntennaAsync(Guid.NewGuid(), new UpdateAntennaDto { Sensibility = 101 }));

        await client.DidNotReceiveWithAnyArgs().UpdateAntennaAsync(default, default!);
    }

    [Fact]
    public async Task UpdateAntennaAsync_WhenSensibilityBelow0_ThrowsArgumentOutOfRange()
    {
        var client = Substitute.For<IGatewayClient>();
        var service = new AntennaService(client);

        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() =>
            service.UpdateAntennaAsync(Guid.NewGuid(), new UpdateAntennaDto { Sensibility = -1 }));

        await client.DidNotReceiveWithAnyArgs().UpdateAntennaAsync(default, default!);
    }

    [Fact]
    public async Task UpdateAntennaAsync_WhenPowerAbove100_ThrowsArgumentOutOfRange()
    {
        var client = Substitute.For<IGatewayClient>();
        var service = new AntennaService(client);

        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() =>
            service.UpdateAntennaAsync(Guid.NewGuid(), new UpdateAntennaDto { Power = 101 }));

        await client.DidNotReceiveWithAnyArgs().UpdateAntennaAsync(default, default!);
    }

    [Fact]
    public async Task UpdateAntennaAsync_WhenPowerBelow0_ThrowsArgumentOutOfRange()
    {
        var client = Substitute.For<IGatewayClient>();
        var service = new AntennaService(client);

        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() =>
            service.UpdateAntennaAsync(Guid.NewGuid(), new UpdateAntennaDto { Power = -1 }));

        await client.DidNotReceiveWithAnyArgs().UpdateAntennaAsync(default, default!);
    }

    [Fact]
    public async Task UpdateAntennaAsync_WhenValidInput_DelegatesToGatewayClient()
    {
        var id = Guid.NewGuid();
        var dto = new UpdateAntennaDto { Status = "On", Sensibility = 80, Power = 30 };
        var expected = new AntennaDto { Id = id, Number = 1, Status = "On", Sensibility = 80, Power = 30 };
        var client = Substitute.For<IGatewayClient>();
        client.UpdateAntennaAsync(id, dto).Returns(expected);
        var service = new AntennaService(client);

        var result = await service.UpdateAntennaAsync(id, dto);

        Assert.Equal(expected, result);
        await client.Received(1).UpdateAntennaAsync(id, dto);
    }

    [Fact]
    public async Task UpdateAntennaAsync_WhenSensibilityIs0_DelegatesToGatewayClient()
    {
        var id = Guid.NewGuid();
        var dto = new UpdateAntennaDto { Sensibility = 0 };
        var expected = new AntennaDto { Id = id, Number = 1, Status = "Off" };
        var client = Substitute.For<IGatewayClient>();
        client.UpdateAntennaAsync(id, Arg.Any<UpdateAntennaDto>()).Returns(expected);
        var service = new AntennaService(client);

        await service.UpdateAntennaAsync(id, dto);

        await client.Received(1).UpdateAntennaAsync(id, Arg.Any<UpdateAntennaDto>());
    }

    [Fact]
    public async Task UpdateAntennaAsync_WhenSensibilityIs100_DelegatesToGatewayClient()
    {
        var id = Guid.NewGuid();
        var dto = new UpdateAntennaDto { Sensibility = 100 };
        var expected = new AntennaDto { Id = id, Number = 1, Status = "Off" };
        var client = Substitute.For<IGatewayClient>();
        client.UpdateAntennaAsync(id, Arg.Any<UpdateAntennaDto>()).Returns(expected);
        var service = new AntennaService(client);

        await service.UpdateAntennaAsync(id, dto);

        await client.Received(1).UpdateAntennaAsync(id, Arg.Any<UpdateAntennaDto>());
    }

    [Fact]
    public async Task UpdateAntennaAsync_WhenPowerIs0_DelegatesToGatewayClient()
    {
        var id = Guid.NewGuid();
        var dto = new UpdateAntennaDto { Power = 0 };
        var expected = new AntennaDto { Id = id, Number = 1, Status = "Off" };
        var client = Substitute.For<IGatewayClient>();
        client.UpdateAntennaAsync(id, Arg.Any<UpdateAntennaDto>()).Returns(expected);
        var service = new AntennaService(client);

        await service.UpdateAntennaAsync(id, dto);

        await client.Received(1).UpdateAntennaAsync(id, Arg.Any<UpdateAntennaDto>());
    }

    [Fact]
    public async Task UpdateAntennaAsync_WhenPowerIs100_DelegatesToGatewayClient()
    {
        var id = Guid.NewGuid();
        var dto = new UpdateAntennaDto { Power = 100 };
        var expected = new AntennaDto { Id = id, Number = 1, Status = "Off" };
        var client = Substitute.For<IGatewayClient>();
        client.UpdateAntennaAsync(id, Arg.Any<UpdateAntennaDto>()).Returns(expected);
        var service = new AntennaService(client);

        await service.UpdateAntennaAsync(id, dto);

        await client.Received(1).UpdateAntennaAsync(id, Arg.Any<UpdateAntennaDto>());
    }
}
