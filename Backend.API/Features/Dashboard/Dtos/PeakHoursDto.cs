namespace Backend.Features.Dashboard;

public class PeakHoursDto
{
    public string? PeakHour { get; set; }
    public int Entries { get; set; }
    public string Range { get; set; } = "Last 24 hours";
}
