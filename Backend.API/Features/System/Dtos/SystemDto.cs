using System.Collections.Generic;

namespace Backend.Features.SystemConfig;

public class SystemDto
{
    public int OccupancyLimit { get; set; }
    public int CurrentOccupancy { get; set; }
    public List<AntennaDto> Antennas { get; set; } = new List<AntennaDto>();
}
