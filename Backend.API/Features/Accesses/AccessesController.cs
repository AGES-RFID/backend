using Microsoft.AspNetCore.Mvc;

namespace Backend.Features.Accesses;

[ApiController]
[Route("api/accesses")]
public class AccessesController(IAccessesService accessService) : ControllerBase
{
    private readonly IAccessesService _accessService = accessService;

    [HttpPost("entry")]
    public async Task<ActionResult<AccessDto>> RegisterEntry([FromBody] CreateAccessDto dto)
    {
        try
        {
            var access = await _accessService.RegisterEntryAsync(dto);
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

    [HttpPost("exit")]
    public async Task<ActionResult<AccessDto>> RegisterExit([FromBody] CreateAccessDto dto)
    {
        try
        {
            var access = await _accessService.RegisterExitAsync(dto);
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
}
