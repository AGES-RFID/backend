using Microsoft.AspNetCore.Mvc;

namespace Backend.Features.Accesses;

[ApiController]
[Route("api/accesses")]
public class AccessesController(IAccessesService accessService) : ControllerBase
{
    private readonly IAccessesService _accessService = accessService;

    [HttpGet]
    public async Task<ActionResult<IEnumerable<AccessDto>>> GetAccesses()
    {
        var accesses = await _accessService.GetAccessesAsync();
        return Ok(accesses);
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
}
