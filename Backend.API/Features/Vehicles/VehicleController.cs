using Microsoft.AspNetCore.Mvc;

namespace Backend.Features.Vehicles;

[ApiController]
[Route("api/vehicles")]
public class VehiclesController(IVehicleService vehicleService) : ControllerBase
{
    private readonly IVehicleService _vehicleService = vehicleService;
    public object? Result;

    [HttpPost]
    public async Task<ActionResult<VehicleDto>> CreateVehicle(CreateVehicleDto dto)
    {
        try
        {
            var vehicle = await _vehicleService.CreateVehicleAsync(dto);
            return CreatedAtAction(nameof(GetVehicle), new { VehicleId = vehicle.VehicleId }, vehicle);
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
        catch (BadHttpRequestException)
        {
            return BadRequest();
        }
        catch (InvalidOperationException)
        {
            return Conflict();
        }
        catch (Exception)
        {
            return StatusCode(500);
        }
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<VehicleDto>>> GetAllVehicles()
    {
        var vehicles = await _vehicleService.GetAllVehiclesAsync();
        return Ok(vehicles);
    }

    [HttpGet("{vehicleId}")]
    public async Task<ActionResult<VehicleDto>> GetVehicle(Guid vehicleId)
    {
        try
        {
            var vehicle = await _vehicleService.GetVehicleAsync(vehicleId);
            return Ok(vehicle);
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
        catch (BadHttpRequestException)
        {
            return BadRequest();
        }
    }

    [HttpPut("{vehicleId}")]
    public async Task<IActionResult> UpdateVehicle(Guid vehicleId, CreateVehicleDto dto)
    {
        try
        {
            var result = await _vehicleService.UpdateVehicleAsync(vehicleId, dto);
            return Ok(result);
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
        catch (BadHttpRequestException)
        {
            return BadRequest();
        }
        catch (InvalidOperationException)
        {
            return Conflict();
        }
        catch (Exception)
        {
            return StatusCode(500);
        }
    }

    [HttpDelete("{vehicleId}")]
    public async Task<IActionResult> DeleteVehicle(Guid vehicleId)
    {
        try
        {
            await _vehicleService.DeleteVehicleAsync(vehicleId);
            return NoContent();
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