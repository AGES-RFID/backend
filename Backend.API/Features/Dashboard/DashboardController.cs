using Microsoft.AspNetCore.Mvc;

namespace Backend.Features.Dashboard;

[ApiController]
[Route("api/dashboard")]
public class DashboardController(IDashboardService dashboardService) : ControllerBase
{
    private readonly IDashboardService _dashboardService = dashboardService;

    [HttpGet("metrics")]
    public async Task<ActionResult<DashboardMetricsDto>> GetMetrics()
    {
        try
        {
            var metrics = await _dashboardService.GetMetricsAsync();
            return Ok(metrics);
        }
        catch (Exception)
        {
            Console.Error.WriteLine("Error calculating dashboard metrics");
            return StatusCode(500, new { error = "Erro interno ao calcular métricas" });
        }
    }
}