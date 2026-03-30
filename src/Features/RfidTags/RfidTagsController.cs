using Microsoft.AspNetCore.Mvc;

namespace Backend.Features.RfidTags;

[ApiController]
[Route("api/rfid-tags")]
public class RfidTagsController(IRfidTagService rfidTagService) : ControllerBase
{
    private readonly IRfidTagService _rfidTagService = rfidTagService;

    [HttpPost]
    public async Task<ActionResult<RfidTagDto>> CreateTag(CreateRfidTagDto dto)
    {
        try
        {
            var tag = await _rfidTagService.CreateTagAsync(dto);
            return CreatedAtAction(nameof(GetTag), new { id = tag.Id }, tag);
        }
        catch (Exception ex)
        {
            return BadRequest(ex.Message);
        }
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<RfidTagDto>> GetTag(int id)
    {
        try
        {
            var tag = await _rfidTagService.GetTagAsync(id);
            if (tag == null)
                return NotFound();
            return Ok(tag);
        }
        catch (Exception)
        {
            return BadRequest("Unable to retrieve RFID tag");
        }
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<RfidTagDto>>> GetAllTags()
    {
        try
        {
            var tags = await _rfidTagService.GetAllTagsAsync();
            return Ok(tags);
        }
        catch (Exception)
        {
            return BadRequest("Unable to retrieve RFID tags");
        }
    }

    [HttpPost("{id}/deactivate")]
    public async Task<ActionResult<RfidTagDto>> DeactivateTag(int id)
    {
        try
        {
            var tag = await _rfidTagService.DeactivateTagAsync(id);
            return Ok(tag);
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
        catch (Exception ex)
        {
            return BadRequest(ex.Message);
        }
    }

    [HttpPost("{id}/reactivate")]
    public async Task<ActionResult<RfidTagDto>> ReactivateTag(int id)
    {
        try
        {
            var tag = await _rfidTagService.ReactivateTagAsync(id);
            return Ok(tag);
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
        catch (Exception ex)
        {
            return BadRequest(ex.Message);
        }
    }
}
