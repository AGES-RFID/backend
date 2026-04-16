namespace Backend.Features.Tags;

public class TagListDto
{
    public string Id { get; set; } = string.Empty;
    public string? UserName { get; set; }
    public string? Plate { get; set; }
    public string Status { get; set; } = string.Empty;
}
