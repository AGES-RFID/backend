namespace Backend.Features.Tags;

public class TagListDto
{
    public Guid TagId { get; set; }
    public required string Tid { get; set; }
    public required string Epc { get; set; }
    public string? UserName { get; set; }
    public string? Plate { get; set; }
    public string Status { get; set; } = string.Empty;
}
