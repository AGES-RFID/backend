using Microsoft.AspNetCore.Mvc;

namespace Backend.Features.Dashboard;

[ApiController]
[Route("dashboard")]
public class DashboardController(IDashboardService dashboardService) : ControllerBase
{
    private readonly IDashboardService _dashboardService = dashboardService;

    [HttpGet("occupancy")]
    public async Task<ActionResult<OccupancyDto>> GetOccupancy()
    {
        try
        {
            var occupancy = await _dashboardService.GetOccupancyAsync();
            return Ok(occupancy);
        }
        catch (Exception)
        {
            return StatusCode(500);
        }
    }
}