namespace Backend.Features.SystemConfig.Models;

public class AntennaConfig
{
    public Guid Id { get; set; }
    public int Number { get; set; }
    public string? Status { get; set; }
    public int Sensibility { get; set; }
    public int Power { get; set; }
}
