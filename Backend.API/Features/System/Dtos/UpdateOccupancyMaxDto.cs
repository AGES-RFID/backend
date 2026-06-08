using System.ComponentModel.DataAnnotations;

namespace Backend.Features.SystemConfig;

public class UpdateOccupancyMaxDto
{
    [Range(1, int.MaxValue, ErrorMessage = "MaxOccupancy must be at least 1.")]
    public int MaxOccupancy { get; set; }
}
