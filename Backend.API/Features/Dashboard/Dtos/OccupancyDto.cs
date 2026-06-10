using Backend.Features.Vehicles;

namespace Backend.Features.Dashboard;

public class OccupancyDto
{
    public int CurrentOccupancy { get; set; }
    public int MaxOccupancy { get; set; }
    public double OccupancyPercentage { get; set; }
    public required IEnumerable<VehicleDto> Vehicles { get; set; }
}
