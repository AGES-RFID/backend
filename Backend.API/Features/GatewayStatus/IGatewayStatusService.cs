namespace Backend.Features.GatewayStatus;

public interface IGatewayStatusService
{
    Task<ReaderStatusResponseDto> SaveStatusAsync(ReaderStatusDto status);
    Task<ReaderStatusResponseDto?> GetLastStatusAsync();
}
