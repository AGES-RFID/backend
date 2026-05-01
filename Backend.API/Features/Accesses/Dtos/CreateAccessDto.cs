namespace Backend.Features.Accesses.Dtos;

public class CreateAccessDto
{
    public required string? Tid { get; set; }
    public required string Epc { get; set; }
    public bool Entrance { get; set; }
    public DateTime Timestamp { get; set; }
}
