using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Backend.Features.ParkingPrices;

[ApiController]
[Route("api/parking-prices")]
public class ParkingPricesController(IParkingPricesService parkingPricesService) : ControllerBase
{
    private readonly IParkingPricesService _parkingPricesService = parkingPricesService;

    [HttpGet]
    public async Task<ActionResult<IEnumerable<ParkingPricesDto>>> GetAllParkingPrices()
    {
        var parkingPrices = await _parkingPricesService.GetAllParkingPricesAsync();
        return Ok(parkingPrices);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<ParkingPricesDto>> GetParkingPrice(Guid id)
    {
        try
        {
            var parkingPrice = await _parkingPricesService.GetParkingPriceAsync(id);
            return Ok(parkingPrice);
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
    }

    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<ParkingPricesDto>> CreateParkingPrice(CreateParkingPriceDto dto)
    {
        var parkingPrice = await _parkingPricesService.CreateParkingPriceAsync(dto);
        return CreatedAtAction(nameof(GetParkingPrice), new { id = parkingPrice.ParkingPriceId }, parkingPrice);
    }

    [HttpPatch("{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<ParkingPricesDto>> UpdateParkingPrice(Guid id, UpdateParkingPriceDto dto)
    {
        try
        {
            var parkingPrice = await _parkingPricesService.UpdateParkingPriceAsync(id, dto);
            return Ok(parkingPrice);
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
    }

    [HttpDelete("{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> DeleteParkingPrice(Guid id)
    {
        try
        {
            await _parkingPricesService.DeleteParkingPriceAsync(id);
            return NoContent();
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
    }

    [HttpGet("/parking-pricing")]
    public async Task<ActionResult<ParkingPricesDto>> GetCurrentParkingPricing()
    {
        try
        {
            var currentPricing = await _parkingPricesService.GetCurrentParkingPricingAsync();
            return Ok(currentPricing);
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
        catch (Exception)
        {
            return StatusCode(500);
        }
    }
}
