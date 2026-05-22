using Backend.Features.ParkingPrices;
using Microsoft.AspNetCore.Mvc;
using NSubstitute;

namespace Backend.Tests.Unit.Features.ParkingPrices;

public class ParkingPricesControllerTests
{
    [Fact]
    public async Task GetAllParkingPrices_WhenCalled_ReturnsOkWithList()
    {
        var expectedList = new List<ParkingPricesDto>
        {
            new() { ParkingPriceId = Guid.NewGuid(), ToleranceMinutes = 15, BasePrice = 15.00m, HourlyRate = 5.00m, ThresholdMinutes = 180 },
            new() { ParkingPriceId = Guid.NewGuid(), ToleranceMinutes = 20, BasePrice = 20.00m, HourlyRate = 6.00m, ThresholdMinutes = 240 }
        };

        var service = Substitute.For<IParkingPricesService>();
        service.GetAllParkingPricesAsync().Returns(expectedList);

        var controller = new ParkingPricesController(service);
        var result = await controller.GetAllParkingPrices();

        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var returnedList = Assert.IsAssignableFrom<IEnumerable<ParkingPricesDto>>(okResult.Value);
        Assert.Equal(2, returnedList.Count());
        await service.Received(1).GetAllParkingPricesAsync();
    }

    [Fact]
    public async Task GetAllParkingPrices_WhenEmpty_ReturnsOkWithEmptyList()
    {
        var service = Substitute.For<IParkingPricesService>();
        service.GetAllParkingPricesAsync().Returns(new List<ParkingPricesDto>());

        var controller = new ParkingPricesController(service);
        var result = await controller.GetAllParkingPrices();

        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var returnedList = Assert.IsAssignableFrom<IEnumerable<ParkingPricesDto>>(okResult.Value);
        Assert.Empty(returnedList);
    }

    [Fact]
    public async Task GetParkingPrice_WhenExists_ReturnsOkWithParkingPrice()
    {
        var id = Guid.NewGuid();
        var expected = new ParkingPricesDto
        {
            ParkingPriceId = id,
            ToleranceMinutes = 15,
            BasePrice = 15.00m,
            HourlyRate = 5.00m,
            ThresholdMinutes = 180,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        var service = Substitute.For<IParkingPricesService>();
        service.GetParkingPriceAsync(id).Returns(expected);

        var controller = new ParkingPricesController(service);
        var result = await controller.GetParkingPrice(id);

        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var returnedDto = Assert.IsType<ParkingPricesDto>(okResult.Value);
        Assert.Equal(expected.ParkingPriceId, returnedDto.ParkingPriceId);
        Assert.Equal(expected.ToleranceMinutes, returnedDto.ToleranceMinutes);
        await service.Received(1).GetParkingPriceAsync(id);
    }

    [Fact]
    public async Task GetParkingPrice_WhenNotExists_ReturnsNotFound()
    {
        var id = Guid.NewGuid();

        var service = Substitute.For<IParkingPricesService>();
        service.GetParkingPriceAsync(id).Returns(Task.FromException<ParkingPricesDto>(
            new KeyNotFoundException($"Parking price with id {id} was not found")));

        var controller = new ParkingPricesController(service);
        var result = await controller.GetParkingPrice(id);

        Assert.IsType<NotFoundResult>(result.Result);
        await service.Received(1).GetParkingPriceAsync(id);
    }

    [Fact]
    public async Task CreateParkingPrice_WhenSuccess_ReturnsCreatedAtAction()
    {
        var dto = new CreateParkingPriceDto
        {
            ToleranceMinutes = 15,
            BasePrice = 15.00m,
            HourlyRate = 5.00m,
            ThresholdMinutes = 180
        };

        var created = new ParkingPricesDto
        {
            ParkingPriceId = Guid.NewGuid(),
            ToleranceMinutes = dto.ToleranceMinutes,
            BasePrice = dto.BasePrice,
            HourlyRate = dto.HourlyRate,
            ThresholdMinutes = dto.ThresholdMinutes,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        var service = Substitute.For<IParkingPricesService>();
        service.CreateParkingPriceAsync(dto).Returns(created);

        var controller = new ParkingPricesController(service);
        var result = await controller.CreateParkingPrice(dto);

        var createdResult = Assert.IsType<CreatedAtActionResult>(result.Result);
        Assert.Equal(nameof(controller.GetParkingPrice), createdResult.ActionName);

        var returnedDto = Assert.IsType<ParkingPricesDto>(createdResult.Value);
        Assert.Equal(created.ParkingPriceId, returnedDto.ParkingPriceId);
        await service.Received(1).CreateParkingPriceAsync(dto);
    }

    [Fact]
    public async Task UpdateParkingPrice_WhenExists_ReturnsOkWithUpdatedPrice()
    {
        var id = Guid.NewGuid();
        var updateDto = new UpdateParkingPriceDto
        {
            ToleranceMinutes = 20,
            BasePrice = 20.00m,
            HourlyRate = 6.00m,
            ThresholdMinutes = 240
        };

        var updated = new ParkingPricesDto
        {
            ParkingPriceId = id,
            ToleranceMinutes = updateDto.ToleranceMinutes ?? 15,
            BasePrice = updateDto.BasePrice ?? 15.00m,
            HourlyRate = updateDto.HourlyRate ?? 5.00m,
            ThresholdMinutes = updateDto.ThresholdMinutes ?? 180,
            CreatedAt = DateTime.UtcNow.AddDays(-1),
            UpdatedAt = DateTime.UtcNow
        };

        var service = Substitute.For<IParkingPricesService>();
        service.UpdateParkingPriceAsync(id, updateDto).Returns(updated);

        var controller = new ParkingPricesController(service);
        var result = await controller.UpdateParkingPrice(id, updateDto);

        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var returnedDto = Assert.IsType<ParkingPricesDto>(okResult.Value);
        Assert.Equal(updated.ToleranceMinutes, returnedDto.ToleranceMinutes);
        await service.Received(1).UpdateParkingPriceAsync(id, updateDto);
    }

    [Fact]
    public async Task UpdateParkingPrice_WhenNotExists_ReturnsNotFound()
    {
        var id = Guid.NewGuid();
        var updateDto = new UpdateParkingPriceDto { BasePrice = 20.00m };

        var service = Substitute.For<IParkingPricesService>();
        service.UpdateParkingPriceAsync(id, updateDto).Returns(Task.FromException<ParkingPricesDto>(
            new KeyNotFoundException($"Parking price with id {id} was not found")));

        var controller = new ParkingPricesController(service);
        var result = await controller.UpdateParkingPrice(id, updateDto);

        Assert.IsType<NotFoundResult>(result.Result);
        await service.Received(1).UpdateParkingPriceAsync(id, updateDto);
    }

    [Fact]
    public async Task UpdateParkingPrice_WithPartialData_UpdatesOnlyProvidedFields()
    {
        var id = Guid.NewGuid();
        var updateDto = new UpdateParkingPriceDto { BasePrice = 25.00m };

        var updated = new ParkingPricesDto
        {
            ParkingPriceId = id,
            ToleranceMinutes = 15,
            BasePrice = 25.00m,
            HourlyRate = 5.00m,
            ThresholdMinutes = 180,
            CreatedAt = DateTime.UtcNow.AddDays(-1),
            UpdatedAt = DateTime.UtcNow
        };

        var service = Substitute.For<IParkingPricesService>();
        service.UpdateParkingPriceAsync(id, updateDto).Returns(updated);

        var controller = new ParkingPricesController(service);
        var result = await controller.UpdateParkingPrice(id, updateDto);

        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var returnedDto = Assert.IsType<ParkingPricesDto>(okResult.Value);
        Assert.Equal(25.00m, returnedDto.BasePrice);
        Assert.Equal(15, returnedDto.ToleranceMinutes); // unchanged
    }

    [Fact]
    public async Task DeleteParkingPrice_WhenExists_ReturnsNoContent()
    {
        var id = Guid.NewGuid();

        var service = Substitute.For<IParkingPricesService>();
        service.DeleteParkingPriceAsync(id).Returns(Task.CompletedTask);

        var controller = new ParkingPricesController(service);
        var result = await controller.DeleteParkingPrice(id);

        Assert.IsType<NoContentResult>(result);
        await service.Received(1).DeleteParkingPriceAsync(id);
    }

    [Fact]
    public async Task DeleteParkingPrice_WhenNotExists_ReturnsNotFound()
    {
        var id = Guid.NewGuid();

        var service = Substitute.For<IParkingPricesService>();
        service.DeleteParkingPriceAsync(id).Returns(Task.FromException(
            new KeyNotFoundException($"Parking price with id {id} was not found")));

        var controller = new ParkingPricesController(service);
        var result = await controller.DeleteParkingPrice(id);

        Assert.IsType<NotFoundResult>(result);
        await service.Received(1).DeleteParkingPriceAsync(id);
    }
    [Fact]
    public async Task GetCurrentParkingPricing_WhenExists_ReturnsOkWithCurrentPrice()
    {
        var expected = new ParkingPricesDto
        {
            ParkingPriceId = Guid.NewGuid(),
            ToleranceMinutes = 15,
            BasePrice = 15.00m,
            HourlyRate = 5.00m,
            ThresholdMinutes = 180,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        var service = Substitute.For<IParkingPricesService>();
        service.GetCurrentParkingPricingAsync().Returns(expected);

        var controller = new ParkingPricesController(service);
        var result = await controller.GetCurrentParkingPricing();

        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var returnedDto = Assert.IsType<ParkingPricesDto>(okResult.Value);
        Assert.Equal(expected.ParkingPriceId, returnedDto.ParkingPriceId);
        await service.Received(1).GetCurrentParkingPricingAsync();
    }

    [Fact]
    public async Task GetCurrentParkingPricing_WhenNotExists_ReturnsNotFound()
    {
        var service = Substitute.For<IParkingPricesService>();
        service.GetCurrentParkingPricingAsync().Returns(Task.FromException<ParkingPricesDto>(
            new KeyNotFoundException("Nenhuma regra de cobrança foi configurada.")));

        var controller = new ParkingPricesController(service);
        var result = await controller.GetCurrentParkingPricing();

        Assert.IsType<NotFoundResult>(result.Result);
        await service.Received(1).GetCurrentParkingPricingAsync();
    }

    [Fact]
    public async Task GetCurrentParkingPricing_OnException_Returns500()
    {
        var service = Substitute.For<IParkingPricesService>();
        service.GetCurrentParkingPricingAsync().Returns(Task.FromException<ParkingPricesDto>(
            new Exception("Database error")));

        var controller = new ParkingPricesController(service);
        var result = await controller.GetCurrentParkingPricing();

        var statusCodeResult = Assert.IsType<StatusCodeResult>(result.Result);
        Assert.Equal(500, statusCodeResult.StatusCode);
        await service.Received(1).GetCurrentParkingPricingAsync();
    }
}
