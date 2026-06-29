namespace Backend.Features.GatewayStatus;

public class ReaderStatusDto
{
    public Guid? ReaderId { get; set; }
    public bool Connected { get; set; }
    public IReadOnlyList<AntennaStatusDto> Antennas { get; set; } = [];
}
