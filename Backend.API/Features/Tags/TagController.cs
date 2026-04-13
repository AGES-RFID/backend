using Microsoft.AspNetCore.Mvc;

namespace Backend.Features.Tags;

[ApiController]
[Route("api/tags")]
public class TagController(ITagService tagService) : ControllerBase
{
    private readonly ITagService _tagService = tagService;

    /// <summary>
    /// POST /tags - Create a new RFID tag
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<TagDto>> CreateTag([FromBody] CreateTagDto dto)
    {
        // Validate payload
        if (!ModelState.IsValid)
        {
            return BadRequest("Invalid request payload");
        }

        try
        {
            var tag = await _tagService.CreateTagAsync(dto);
            return CreatedAtAction(nameof(GetAllTags), new { tagId = tag.TagId }, tag);
        }
        catch (TagConflictException ex)
        {
            return Conflict(ex.Message);
        }
        catch (Exception)
        {
            return StatusCode(StatusCodes.Status500InternalServerError, "An unexpected error occurred while creating the tag");
        }
    }

    /// <summary>
    /// GET /tags - Get all RFID tags with optional status filter
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<IEnumerable<TagListDto>>> GetAllTags([FromQuery] string? status)
    {
        try
        {
            var tags = await _tagService.GetAllTagsAsync(status);
            return Ok(tags);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ex.Message);
        }
        catch (Exception)
        {
            return StatusCode(StatusCodes.Status500InternalServerError, "An unexpected error occurred while retrieving tags");
        }
    }

    /// <summary>
    /// PATCH /tags/{tagId}/deactivate - Deactivate an RFID tag
    /// </summary>
    [HttpPatch("{tagId}/deactivate")]
    public async Task<ActionResult<TagDto>> DeactivateTag(string tagId)
    {
        try
        {
            var tag = await _tagService.DeactivateTagAsync(tagId);
            return Ok(tag);
        }
        catch (KeyNotFoundException)
        {
            return NotFound($"Tag with id {tagId} not found");
        }
        catch (TagConflictException ex)
        {
            return Conflict(ex.Message);
        }
        catch (Exception)
        {
            return StatusCode(StatusCodes.Status500InternalServerError, "An unexpected error occurred while deactivating the tag");
        }
    }

    /// <summary>
    /// PATCH /tags/{tagId}/assign-vehicle - Assign an RFID tag to a vehicle
    /// </summary>
    [HttpPatch("{tagId}/assign-vehicle")]
    public async Task<ActionResult<TagDto>> AssignVehicle(string tagId, [FromBody] AssignVehicleDto dto)
    {
        // Validate payload
        if (!ModelState.IsValid)
        {
            return BadRequest("Invalid request payload");
        }

        try
        {
            var tag = await _tagService.AssignVehicleAsync(tagId, dto);
            return Ok(tag);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(ex.Message);
        }
        catch (TagConflictException ex)
        {
            return Conflict(ex.Message);
        }
        catch (Exception)
        {
            return StatusCode(StatusCodes.Status500InternalServerError, "An unexpected error occurred while assigning the vehicle");
        }
    }
}

