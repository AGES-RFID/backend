using Backend.Features.Vehicles;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using NSubstitute;

namespace Backend.Tests.Unit.Features.Vehicles;

public class VehicleControllerTests
{
    [Fact]
    public async Task CreateVehicle_WhenSuccess_ReturnsCreatedAtAction()
    {
        var dto = new CreateVehicleDto { Plate = "AAA9A99", Brand = "Honda", Model = "HRV", UserId = Guid.NewGuid() };
        var expected = new VehicleDto { VehicleId = Guid.NewGuid(), Plate = dto.Plate, Brand = dto.Brand, Model = dto.Model, UserId = dto.UserId };

        var vehicleService = Substitute.For<IVehicleService>();
        vehicleService.CreateVehicleAsync(dto).Returns(expected);

        var controller = new VehiclesController(vehicleService);

        var result = await controller.CreateVehicle(dto);

        var createdAtResult = Assert.IsType<CreatedAtActionResult>(result.Result);
        Assert.Equal(nameof(controller.GetVehicle), createdAtResult.ActionName);

        var returnedDto = Assert.IsType<VehicleDto>(createdAtResult.Value);
        Assert.Equal(expected.VehicleId, returnedDto.VehicleId);
    }

    [Fact]
    public async Task CreateVehicle_WhenServiceThrowsInvalidOperationException_ReturnsConflict()
    {
        var dto = new CreateVehicleDto { Plate = "AAA9A99", Brand = "Honda", Model = "HRV", UserId = Guid.NewGuid() };

        var vehicleService = Substitute.For<IVehicleService>();
        vehicleService.CreateVehicleAsync(dto).Returns(Task.FromException<VehicleDto>(new InvalidOperationException()));

        var controller = new VehiclesController(vehicleService);

        var result = await controller.CreateVehicle(dto);

        Assert.IsType<ConflictResult>(result.Result);
    }

    [Fact]
    public async Task GetAllVehicles_WhenCalled_ReturnsOkWithList()
    {
        var expectedList = new List<VehicleDto>
        {
            new VehicleDto { VehicleId = Guid.NewGuid(), Plate = "AAA0000", Brand = "Honda", Model = "Civic", UserId = Guid.NewGuid() }
        };

        var vehicleService = Substitute.For<IVehicleService>();
        vehicleService.GetAllVehiclesAsync(false).Returns(expectedList);

        var controller = new VehiclesController(vehicleService);

        var result = await controller.GetAllVehicles();

        await vehicleService.Received(1).GetAllVehiclesAsync(false);

        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var actualList = Assert.IsAssignableFrom<IEnumerable<VehicleDto>>(okResult.Value);
        Assert.Single(actualList);
    }

    [Fact]
    public async Task GetVehicle_WhenServiceThrowsBadHttpRequest_ReturnsBadRequest()
    {
        var VehicleId = Guid.NewGuid();

        var vehicleService = Substitute.For<IVehicleService>();
        vehicleService.GetVehicleAsync(VehicleId)
            .Returns(Task.FromException<VehicleDto>(new BadHttpRequestException("Invalid or missing information.")));

        var controller = new VehiclesController(vehicleService);

        var result = await controller.GetVehicle(VehicleId);

        Assert.IsType<BadRequestResult>(result.Result);
        await vehicleService.Received(1).GetVehicleAsync(VehicleId);
    }

    [Fact]
    public async Task GetVehicle_WhenServiceThrowsKeyNotFoundException_ReturnsNotFound()
    {
        var VehicleId = Guid.NewGuid();

        var vehicleService = Substitute.For<IVehicleService>();
        vehicleService.GetVehicleAsync(VehicleId)
            .Returns(Task.FromException<VehicleDto>(new KeyNotFoundException("Not Found")));

        var controller = new VehiclesController(vehicleService);

        var result = await controller.GetVehicle(VehicleId);

        Assert.IsType<NotFoundResult>(result.Result);
        await vehicleService.Received(1).GetVehicleAsync(VehicleId);
    }

    [Fact]
    public async Task GetVehicle_WhenServiceReturnsVehicle_ReturnsOkWithVehicle()
    {
        var vehicleId = Guid.NewGuid();

        var expected = new VehicleDto
        {
            VehicleId = vehicleId,
            UserId = Guid.NewGuid(),
            Plate = "AAA9A99",
            Model = "HRV",
            Brand = "Honda"
        };

        var vehicleService = Substitute.For<IVehicleService>();
        vehicleService.GetVehicleAsync(vehicleId).Returns(expected);

        var controller = new VehiclesController(vehicleService);

        var result = await controller.GetVehicle(vehicleId);
        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var dto = Assert.IsType<VehicleDto>(ok.Value);

        Assert.Equal(expected.Plate, dto.Plate);
        Assert.Equal(expected.Model, dto.Model);
        Assert.Equal(expected.Brand, dto.Brand);
    }

    [Fact]
    public async Task SearchVehicleByPlate_WhenPlateIsNullOrWhiteSpace_ReturnsBadRequest()
    {
        var vehicleService = Substitute.For<IVehicleService>();
        var controller = new VehiclesController(vehicleService);

        var result = await controller.SearchVehicleByPlate("   ");

        Assert.IsType<BadRequestObjectResult>(result.Result);
    }

    [Fact]
    public async Task SearchVehicleByPlate_WhenServiceThrowsKeyNotFoundException_ReturnsNotFound()
    {
        var vehicleService = Substitute.For<IVehicleService>();
        vehicleService.GetVehicleByPlateAsync("NOTFND")
            .Returns(Task.FromException<VehicleSearchResponseDto>(new KeyNotFoundException()));

        var controller = new VehiclesController(vehicleService);

        var result = await controller.SearchVehicleByPlate("NOTFND");

        Assert.IsType<NotFoundObjectResult>(result.Result);
    }

    [Fact]
    public async Task SearchVehicleByPlate_WhenSuccess_ReturnsOk()
    {
        var expected = new VehicleSearchResponseDto { VehicleId = Guid.NewGuid(), OwnerName = "Test", Plate = "AAA9A99" };
        var vehicleService = Substitute.For<IVehicleService>();
        vehicleService.GetVehicleByPlateAsync("AAA9A99").Returns(expected);

        var controller = new VehiclesController(vehicleService);

        var result = await controller.SearchVehicleByPlate("AAA9A99");

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        Assert.Equal(expected, ok.Value);
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
        var expected = new VehicleDto
        {
            VehicleId = vehicleId,
            UserId = dto.UserId,
            Plate = dto.Plate,
            Brand = dto.Brand,
            Model = dto.Model
        };

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
