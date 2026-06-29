namespace Backend.Features.GatewayStatus;

public class ReaderStatus
{
    public Guid ReaderId { get; set; }
    public string ReaderStatusValue { get; set; } = string.Empty;
    public DateTime LastPing { get; set; }
    public List<ReaderAntennaStatus> AntennaList { get; set; } = [];
}
