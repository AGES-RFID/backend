namespace Backend.Features.Tags;

public class BulkCreateTagsResultDto
{
    public int CreatedCount { get; set; }
    public int ErrorCount => Errors.Count;
    public List<TagDto> CreatedTags { get; set; } = [];
    public List<string> Errors { get; set; } = [];
}
