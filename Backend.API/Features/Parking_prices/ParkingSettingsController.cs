using Microsoft.AspNetCore.Mvc;

namespace Backend.Features.ParkingSettings;

[ApiController]
[Route("api/parking-settings")]
public class ParkingSettingsController(IParkingSettingsService parkingSettingsService) : ControllerBase
{
    private readonly IParkingSettingsService _parkingSettingsService = parkingSettingsService;

    [HttpGet]
    public async Task<ActionResult<ParkingSettingsDto>> GetSettings()
    {
        try
        {
            var settings = await _parkingSettingsService.GetSettingsAsync();
            return Ok(settings);
        }
        catch (ParkingSettingsNotFoundException)
        {
            return NotFound();
        }
        catch (Exception)
        {
            return StatusCode(500, new { error = "Erro interno ao buscar configurações" });
        }
    }

    [HttpPatch]
    public async Task<ActionResult<ParkingSettingsDto>> UpdateSettings(UpdateParkingSettingsDto dto)
    {
        try
        {
            var settings = await _parkingSettingsService.UpdateSettingsAsync(dto);
            return Ok(settings);
        }
        catch (ParkingSettingsNotFoundException)
        {
            return NotFound();
        }
        catch (Exception)
        {
            return StatusCode(500, new { error = "Erro interno ao atualizar configurações" });
        }
    }
}