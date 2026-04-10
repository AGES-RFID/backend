using Backend.Features.Vehicles;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using NSubstitute;

namespace Backend.Tests.Unit.Features.Vehicles;

public class VehicleControllerTests
{
    [Fact]
    public async Task GetVehicle_WhenServiceThrowsBadHttpRequest_ReturnsBadRequest()
    {
        // Arrange
        var VehicleId = Guid.NewGuid();

        var vehicleService = Substitute.For<IVehicleService>();
        vehicleService.GetVehicleAsync(VehicleId)
            .Returns(Task.FromException<VehicleDto>(new BadHttpRequestException("Invalid or missing information.")));

        var controller = new VehiclesController(vehicleService);

        // Act
        var result = await controller.GetVehicle(VehicleId);

        // Assert
        Assert.IsType<BadRequestResult>(result.Result);
        await vehicleService.Received(1).GetVehicleAsync(VehicleId);
    }


    [Fact]
    public async Task GetVehicle_WhenServiceThrowsKeyNotFoundException_ReturnsNotFound()
    {
        // Arrange
        var VehicleId = Guid.NewGuid();

        var vehicleService = Substitute.For<IVehicleService>();
        vehicleService.GetVehicleAsync(VehicleId)
            .Returns(Task.FromException<VehicleDto>(new KeyNotFoundException("Not Found")));

        var controller = new VehiclesController(vehicleService);

        // Act
        var result = await controller.GetVehicle(VehicleId);

        // Assert
        Assert.IsType<NotFoundResult>(result.Result);
        await vehicleService.Received(1).GetVehicleAsync(VehicleId);
    }
    [Fact]
    public async Task GetVehicle_WhenServiceReturnsVehicle_ReturnsOkWithVehicle()
    {
        // Given
        var vehicleId = Guid.NewGuid();

        var expected = new VehicleDto
        {
            VehicleId = vehicleId,
            Plate = "AAA9A99",
            Model = "HRV",
            Brand = "Honda"
        };

        var vehicleService = Substitute.For<IVehicleService>();
        vehicleService.GetVehicleAsync(vehicleId).Returns(expected);

        var controller = new VehiclesController(vehicleService);

        // When
        var result = await controller.GetVehicle(vehicleId);
        // Then
        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var dto = Assert.IsType<VehicleDto>(ok.Value);

        Assert.Equal(expected.Plate, dto.Plate);
        Assert.Equal(expected.Model, dto.Model);
        Assert.Equal(expected.Brand, dto.Brand);
    }

    [Fact]
    public async Task UpdateVehicle_WhenServiceThrowsKeyNotFoundException_ReturnsNotFound()
    {
        var vehicleId = Guid.NewGuid();
        var dto = new CreateVehicleDto { Plate = "AAA9A99", Brand = "Honda", Model = "HRV", UserId = Guid.NewGuid() };

        var vehicleService = Substitute.For<IVehicleService>();
        vehicleService.UpdateVehicleAsync(vehicleId, dto).Returns(Task.FromException<VehicleDto>(new KeyNotFoundException()));

        var controller = new VehiclesController(vehicleService);

        var result = await controller.UpdateVehicle(vehicleId, dto);

        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task UpdateVehicle_WhenServiceThrowsInvalidOperationException_ReturnsConflict()
    {
        var vehicleId = Guid.NewGuid();
        var dto = new CreateVehicleDto { Plate = "AAA9A99", Brand = "Honda", Model = "HRV", UserId = Guid.NewGuid() };

        var vehicleService = Substitute.For<IVehicleService>();
        vehicleService.UpdateVehicleAsync(vehicleId, dto).Returns(Task.FromException<VehicleDto>(new InvalidOperationException()));

        var controller = new VehiclesController(vehicleService);

        var result = await controller.UpdateVehicle(vehicleId, dto);

        Assert.IsType<ConflictResult>(result);
    }

    [Fact]
    public async Task UpdateVehicle_WhenSuccess_ReturnsOk()
    {
        var vehicleId = Guid.NewGuid();
        var dto = new CreateVehicleDto { Plate = "AAA9A99", Brand = "Honda", Model = "HRV", UserId = Guid.NewGuid() };
        var expected = new VehicleDto { VehicleId = vehicleId, Plate = dto.Plate, Brand = dto.Brand, Model = dto.Model };

        var vehicleService = Substitute.For<IVehicleService>();
        vehicleService.UpdateVehicleAsync(vehicleId, dto).Returns(expected);

        var controller = new VehiclesController(vehicleService);

        var result = await controller.UpdateVehicle(vehicleId, dto);

        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.Equal(expected, ok.Value);
    }

    [Fact]
    public async Task DeleteVehicle_WhenServiceThrowsKeyNotFoundException_ReturnsNotFound()
    {
        var vehicleId = Guid.NewGuid();
        var vehicleService = Substitute.For<IVehicleService>();
        vehicleService.DeleteVehicleAsync(vehicleId).Returns(Task.FromException(new KeyNotFoundException()));

        var controller = new VehiclesController(vehicleService);
        var result = await controller.DeleteVehicle(vehicleId);

        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task DeleteVehicle_WhenSuccess_ReturnsNoContent()
    {
        var vehicleId = Guid.NewGuid();
        var vehicleService = Substitute.For<IVehicleService>();
        vehicleService.DeleteVehicleAsync(vehicleId).Returns(Task.CompletedTask);

        var controller = new VehiclesController(vehicleService);
        var result = await controller.DeleteVehicle(vehicleId);

        Assert.IsType<NoContentResult>(result);
    }
}