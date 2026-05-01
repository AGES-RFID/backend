namespace Backend.Features.Accesses.Dtos;

public class AccessDto
{
    public Guid AccessId { get; set; }
    public required string TagId { get; set; }
    public required string? Epc { get; set; }
    public required string Type { get; set; }
    public required DateTime Timestamp { get; set; }
}
