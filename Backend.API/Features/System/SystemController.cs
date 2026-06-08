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

    [HttpGet("occupancy-limit")]
    public async Task<ActionResult<OccupancyLimitDto>> GetOccupancyLimit()
    {
        try
        {
            var maxOccupancy = await _settingsService.GetAsync(MaxOccupancyKey, DefaultMaxOccupancy);
            return Ok(new OccupancyLimitDto { MaxOccupancy = maxOccupancy });
        }
        catch (Exception)
        {
            return Problem();
        }
    }

    [HttpPut("occupancy-limit")]
    public async Task<ActionResult<OccupancyLimitDto>> UpdateOccupancyLimit(UpdateOccupancyLimitDto dto)
    {
        try
        {
            await _settingsService.SetAsync(MaxOccupancyKey, dto.MaxOccupancy.ToString());
            return Ok(new OccupancyLimitDto { MaxOccupancy = dto.MaxOccupancy });
        }
        catch (Exception)
        {
            return Problem();
        }
    }
}
