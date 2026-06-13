using System.ComponentModel.DataAnnotations;

namespace Backend.Features.Antennas;

public class UpdateAntennaDto
{
    [AllowedValues("On", "Off", null)]
    public string? Status { get; set; }

    [Range(0, 100)]
    public int? Sensibility { get; set; }

    [Range(0, 100)]
    public int? Power { get; set; }
}
