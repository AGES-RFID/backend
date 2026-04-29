using Microsoft.AspNetCore.Mvc;

namespace Backend.Features.Vehicles;

[ApiController]
[Route("api/vehicles")]
public class VehiclesController(IVehicleService vehicleService) : ControllerBase
{
    private readonly IVehicleService _vehicleService = vehicleService;
    public object? Result;

    [HttpGet]
    public async Task<ActionResult<IEnumerable<VehicleDto>>> GetAllVehicles([FromQuery] string? include = null)
    {
        if (!TryParseInclude(include, out var includeUsers))
        {
            return BadRequest(new { error = "O parâmetro 'include' é inválido." });
        }

        var vehicles = await _vehicleService.GetAllVehiclesAsync(includeUsers);
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

    private static bool TryParseInclude(string? include, out bool includeUsers)
    {
        includeUsers = false;
        if (string.IsNullOrWhiteSpace(include))
        {
            return true;
        }

        var tokens = include.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        foreach (var token in tokens)
        {
            if (string.Equals(token, "users", StringComparison.OrdinalIgnoreCase))
            {
                includeUsers = true;
                continue;
            }

            return false;
        }

        return true;
    }
}
