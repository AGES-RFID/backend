using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Backend.Features.Antennas;

[ApiController]
[Route("api/antennas")]
[Authorize(Roles = "Admin")]
public class AntennaController(IAntennaService antennaService) : ControllerBase
{
    private readonly IAntennaService _antennaService = antennaService;

    [HttpGet]
    public async Task<ActionResult<List<AntennaDto>>> GetAntennas()
    {
        try
        {
            var antennas = await _antennaService.GetAntennasAsync();
            return Ok(antennas);
        }
        catch (GatewayException ex)
        {
            if (ex.StatusCode >= 400 && ex.StatusCode < 500)
                return UnprocessableEntity(new { error = $"Gateway rejected the request: {ex.StatusCode}" });
            return StatusCode(502, new { error = $"Gateway error: {ex.StatusCode}" });
        }
        catch (Exception)
        {
            return Problem();
        }
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<AntennaDto>> GetAntenna(Guid id)
    {
        try
        {
            var antenna = await _antennaService.GetAntennaAsync(id);
            return Ok(antenna);
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
        catch (GatewayException ex)
        {
            if (ex.StatusCode >= 400 && ex.StatusCode < 500)
                return UnprocessableEntity(new { error = $"Gateway rejected the request: {ex.StatusCode}" });
            return StatusCode(502, new { error = $"Gateway error: {ex.StatusCode}" });
        }
        catch (Exception)
        {
            return Problem();
        }
    }

    [HttpPut("{id}")]
    public async Task<ActionResult<AntennaDto>> UpdateAntenna(Guid id, UpdateAntennaDto dto)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        try
        {
            var antenna = await _antennaService.UpdateAntennaAsync(id, dto);
            return Ok(antenna);
        }
        catch (ArgumentOutOfRangeException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
        catch (GatewayException ex)
        {
            if (ex.StatusCode >= 400 && ex.StatusCode < 500)
                return UnprocessableEntity(new { error = $"Gateway rejected the request: {ex.StatusCode}" });
            return StatusCode(502, new { error = $"Gateway error: {ex.StatusCode}" });
        }
        catch (Exception)
        {
            return Problem();
        }
    }
}
