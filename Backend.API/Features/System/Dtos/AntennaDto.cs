namespace Backend.Features.SystemConfig;

public class AntennaDto
{
    public Guid Id { get; set; }
    public int Number { get; set; }
    public string Status { get; set; } = string.Empty;
    public int Sensibility { get; set; }
    public int Power { get; set; }
}
