using Microsoft.AspNetCore.Mvc;

namespace Backend.Features.Admins;

[ApiController]
[Route("api/admins")]
public class AdminsController(IAdminService adminService) : ControllerBase
{
    private readonly IAdminService _adminService = adminService;

    [HttpPost("register")]
    public async Task<ActionResult<AdminDto>> RegisterAdmin(CreateAdminDto dto)
    {
        var admin = await _adminService.CreateAdminAsync(dto);
        return CreatedAtAction(nameof(GetAdmin), new { id = admin.Id }, admin);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<AdminDto>> GetAdmin(int id)
    {
        try
        {
            var admin = await _adminService.GetAdminAsync(id);
            return Ok(admin);
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<AdminDto>>> GetAllAdmins()
    {
        var admins = await _adminService.GetAllAdminsAsync();
        return Ok(admins);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateAdmin(int id, CreateAdminDto dto)
    {
        try
        {
            await _adminService.UpdateAdminAsync(id, dto);
            return NoContent();
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
    }
}
