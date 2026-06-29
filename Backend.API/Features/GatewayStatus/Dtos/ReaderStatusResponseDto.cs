namespace Backend.Features.GatewayStatus;

public sealed class ReaderStatusResponseDto : ReaderStatusDto
{
    public string ReaderStatus { get; set; } = string.Empty;
    public IReadOnlyList<AntennaStatusDto> DesiredAntennas { get; set; } = [];
    public DateTime ReceivedAtUtc { get; set; }
}
