using Backend.Features.Settings;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Backend.Features.SystemConfig;

[ApiController]
[Route("api/system")]
[Authorize(Roles = "Admin")]
public class SystemController(ISettingsService settingsService) : ControllerBase
{
    private const string MaxOccupancyKey = "max_occupancy";
    private const int DefaultMaxOccupancy = 100;
    private readonly ISettingsService _settingsService = settingsService;

    [HttpGet("max-occupancy")]
    public async Task<ActionResult<MaxOccupancyDto>> GetMaxOccupancy()
    {
        try
        {
            var maxOccupancy = await _settingsService.GetAsync(MaxOccupancyKey, DefaultMaxOccupancy);
            return Ok(new MaxOccupancyDto { MaxOccupancy = maxOccupancy });
        }
        catch (Exception)
        {
            return Problem();
        }
    }

    [HttpPut("max-occupancy")]
    public async Task<ActionResult<MaxOccupancyDto>> UpdateMaxOccupancy(UpdateMaxOccupancyDto dto)
    {
        try
        {
            await _settingsService.SetAsync(MaxOccupancyKey, dto.MaxOccupancy.ToString());
            return Ok(new MaxOccupancyDto { MaxOccupancy = dto.MaxOccupancy });
        }
        catch (Exception)
        {
            return Problem();
        }
    }

    [HttpGet]
    public async Task<ActionResult<SystemDto>> GetSystem([FromServices] ISystemService systemService)
    {
        try
        {
            var system = await systemService.GetSystemAsync();
            return Ok(system);
        }
        catch (Exception)
        {
            return Problem();
        }
    }

    [HttpGet("antennas")]
    public async Task<ActionResult<IEnumerable<AntennaDto>>> GetAntennas([FromServices] ISystemService systemService)
    {
        try
        {
            var system = await systemService.GetSystemAsync();
            return Ok(system.Antennas);
        }
        catch (Exception)
        {
            return Problem();
        }
    }
}
