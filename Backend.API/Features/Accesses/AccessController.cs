using Backend.Features.Accesses.Dtos;
using Microsoft.AspNetCore.Mvc;

namespace Backend.Features.Accesses;

[ApiController]
[Route("api/accesses")]
public class AccessController(IAccessService accessService) : ControllerBase
{
    private readonly IAccessService _accessService = accessService;

    [HttpPost]
    public async Task<ActionResult<AccessDto>> CreateAccess([FromBody] CreateAccessDto dto)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest("Invalid request payload");
        }

        try
        {
            var access = await _accessService.CreateAccessAsync(dto);
            return CreatedAtAction(nameof(CreateAccess), new { accessId = access.AccessId }, access);
        }
        catch (Exception)
        {
            return StatusCode(StatusCodes.Status500InternalServerError, "An unexpected error occurred while creating the access");
        }
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<AccessDto>>> GetAllAccesses()
    {
        try
        {
            var accesses = await _accessService.GetAllAccessesAsync();
            return Ok(accesses);
        }
        catch (Exception)
        {
            return StatusCode(StatusCodes.Status500InternalServerError, "An unexpected error occurred while retrieving accesses");
        }
    }
}
