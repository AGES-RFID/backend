namespace Backend.Features.GatewayStatus;

public sealed class AntennaStatusDto
{
    public ushort Port { get; set; }
    public bool Connected { get; set; }
    public double Power { get; set; }
    public double Sensitivity { get; set; }
    public string AntennaStatus => Connected ? "connected" : "disconnected";
}
