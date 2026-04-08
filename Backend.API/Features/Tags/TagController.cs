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
            return BadRequest(new
            {
                message = "Invalid request payload"
            });
        }

        try
        {
            var tag = await _tagService.CreateTagAsync(dto);
            return StatusCode(StatusCodes.Status201Created, tag);
        }
        catch (TagConflictException ex)
        {
            return StatusCode(StatusCodes.Status409Conflict, new
            {
                message = ex.Message
            });
        }
        catch (Exception)
        {
            return StatusCode(StatusCodes.Status500InternalServerError, new
            {
                message = "An unexpected error occurred while creating the tag"
            });
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
            return BadRequest(new
            {
                message = ex.Message
            });
        }
        catch (Exception)
        {
            return StatusCode(StatusCodes.Status500InternalServerError, new
            {
                message = "An unexpected error occurred while retrieving tags"
            });
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
            return NotFound(new
            {
                message = $"Tag with id {tagId} not found"
            });
        }
        catch (TagConflictException ex)
        {
            return StatusCode(StatusCodes.Status409Conflict, new
            {
                message = ex.Message
            });
        }
        catch (Exception)
        {
            return StatusCode(StatusCodes.Status500InternalServerError, new
            {
                message = "An unexpected error occurred while deactivating the tag"
            });
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
            return BadRequest(new
            {
                message = "Invalid request payload"
            });
        }

        try
        {
            var tag = await _tagService.AssignVehicleAsync(tagId, dto);
            return Ok(tag);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new
            {
                message = ex.Message
            });
        }
        catch (TagConflictException ex)
        {
            return StatusCode(StatusCodes.Status409Conflict, new
            {
                message = ex.Message
            });
        }
        catch (Exception)
        {
            return StatusCode(StatusCodes.Status500InternalServerError, new
            {
                message = "An unexpected error occurred while assigning the vehicle"
            });
        }
    }
}

