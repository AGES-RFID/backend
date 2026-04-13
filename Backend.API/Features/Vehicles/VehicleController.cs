using Microsoft.AspNetCore.Mvc;

namespace Backend.Features.Vehicles;

[ApiController]
[Route("api/vehicles")]
public class VehiclesController(IVehicleService vehicleService) : ControllerBase
{
    private readonly IVehicleService _vehicleService = vehicleService;
    public object? Result;

    [HttpGet]
    public async Task<ActionResult<IEnumerable<VehicleDto>>> GetAllVehicles()
    {
        var vehicles = await _vehicleService.GetAllVehiclesAsync();
        return Ok(vehicles);
    }

    [HttpGet("search")]
    public async Task<ActionResult<VehicleSearchResponseDto>> SearchVehicleByPlate([FromQuery] string? plate)
    {
        if (string.IsNullOrWhiteSpace(plate))
            return BadRequest(new { error = "O parâmetro de busca 'plate' é obrigatório." });

        try
        {
            var result = await _vehicleService.GetVehicleByPlateAsync(plate);
            return Ok(result);
        }
        catch (KeyNotFoundException)
        {
            return NotFound(new { error = "Veículo não encontrado." });
        }
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
