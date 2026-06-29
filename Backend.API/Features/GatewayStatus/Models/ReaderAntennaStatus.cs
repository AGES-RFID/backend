namespace Backend.Features.GatewayStatus;

public class ReaderAntennaStatus
{
    public int Port { get; set; }
    public double Power { get; set; }
    public double Sensitivity { get; set; }
    public string AntennaStatus { get; set; } = string.Empty;
}
