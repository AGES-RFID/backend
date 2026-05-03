using Backend.Features.Vehicles;

namespace Backend.Features.Dashboard;

public class OccupancyDto
{
    public int CurrentOccupancy { get; set; }
    public required IEnumerable<VehicleDto> Vehicles { get; set; }
}
