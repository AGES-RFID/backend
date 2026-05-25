namespace Backend.Features.Accesses;

public class CreateAccessDto
{
    public required string Tid { get; init; }
    public required string Epc { get; init; }
    public bool Entrance { get; init; }
}
