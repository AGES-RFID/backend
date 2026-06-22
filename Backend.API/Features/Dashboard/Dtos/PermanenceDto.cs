namespace Backend.Features.Dashboard;

public class PermanenceDto
{
    public required string RfidTag { get; set; }
    public required string Plate { get; set; }
    public int MinutesParked { get; set; }
}
