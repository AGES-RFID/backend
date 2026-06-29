using Microsoft.AspNetCore.Mvc;

namespace Backend.Features.GatewayStatus;

[ApiController]
[Route("api/gateway")]
public class GatewayStatusController(IGatewayStatusService gatewayStatusService) : ControllerBase
{
    private readonly IGatewayStatusService _gatewayStatusService = gatewayStatusService;

    [HttpPost()]
    public async Task<ActionResult<ReaderStatusResponseDto>> ReceiveStatus([FromBody] ReaderStatusDto status)
    {
        if (!ModelState.IsValid)
            return BadRequest(new { error = "Invalid gateway status payload." });

        var savedStatus = await _gatewayStatusService.SaveStatusAsync(status);
        return Ok(savedStatus);
    }

    [HttpGet()]
    public async Task<ActionResult<ReaderStatusResponseDto>> GetLastStatus()
    {
        var status = await _gatewayStatusService.GetLastStatusAsync();
        return status is null ? NotFound(new { error = "Gateway status has not been received yet." }) : Ok(status);
    }

    [HttpPost("configuration")]
    public async Task<ActionResult<ReaderStatusResponseDto>> SyncAntennaConfiguration([FromBody] ReaderStatusDto currentConfiguration)
    {
        if (!ModelState.IsValid)
            return BadRequest(new { error = "Invalid antenna configuration payload." });

        var savedStatus = await _gatewayStatusService.ConfirmConfigurationAsync(currentConfiguration);
        return Ok(savedStatus);
    }
}
