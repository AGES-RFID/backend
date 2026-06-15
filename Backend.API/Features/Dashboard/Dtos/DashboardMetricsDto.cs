using Backend.Features.Accesses;

namespace Backend.Features.Dashboard;

public class DashboardMetricsDto
{
    public int EntriesLastHour { get; set; }
    public int ExitsLastHour { get; set; }
    public string? PeakEntryTime { get; set; }
    public int PeakEntryHour { get; set; }
    public int CurrentOccupancy { get; set; }
    public int MaxOccupancy { get; set; }
    public IEnumerable<AccessDto> Accesses { get; set; } = [];  // ✅
    public DateTime UpdatedAt { get; set; }                      // ✅
}