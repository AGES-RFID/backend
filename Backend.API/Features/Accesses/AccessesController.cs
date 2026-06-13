using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Backend.Features.Accesses;

[ApiController]
[Route("api/accesses")]
public class AccessesController(IAccessesService accessService) : ControllerBase
{
    private readonly IAccessesService _accessService = accessService;

    [HttpGet]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<IEnumerable<AccessDto>>> GetAccesses([FromQuery] string? accessType = null)
    {
        if (!TryParseAccessType(accessType, out var parsedType))
            return BadRequest(new { error = "O parâmetro 'accessType' é inválido." });

        var accesses = await _accessService.GetAccessesAsync(parsedType);
        return Ok(accesses);
    }
    [HttpGet("timeseries")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<TimeseriesResponseDto>> GetTimeseries()
    {
        var timeseries = await _accessService.GetTimeSeriesAsync();
        return Ok(timeseries);
    }
    [HttpPost]
    public async Task<ActionResult<AccessDto>> RegisterAccess([FromBody] CreateAccessDto dto)
    {
        try
        {
            var access = await _accessService.RegisterAccessAsync(dto);
            return StatusCode(StatusCodes.Status201Created, access);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { error = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { error = ex.Message });
        }
        catch (Exception)
        {
            return StatusCode(StatusCodes.Status500InternalServerError, new { error = "Ocorreu um erro interno no servidor." });
        }
    }

    private static bool TryParseAccessType(string? accessType, out AccessType? parsedType)
    {
        parsedType = null;

        if (string.IsNullOrWhiteSpace(accessType))
            return true;

        if (!Enum.TryParse<AccessType>(accessType, ignoreCase: true, out var parsed))
            return false;

        parsedType = parsed;
        return true;
    }
}
