namespace Backend.Features.Dashboard;

public class DashboardMetricsDto
{
    public int EntriesLastHour { get; set; }
    public int ExitsLastHour { get; set; }
    public string? PeakEntryTime { get; set; }
}