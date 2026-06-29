namespace Backend.Features.SystemConfig;

public class UpdateAntennaDto
{
    public string Status { get; set; } = string.Empty;
    public double? Sensibility { get; set; }
    public double? Power { get; set; }
}
