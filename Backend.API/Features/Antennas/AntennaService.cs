namespace Backend.Features.Antennas;

public interface IAntennaService
{
    Task<List<AntennaDto>> GetAntennasAsync();
    Task<AntennaDto> GetAntennaAsync(Guid id);
    Task<AntennaDto> UpdateAntennaAsync(Guid id, UpdateAntennaDto dto);
}

public class AntennaService(IGatewayClient gatewayClient) : IAntennaService
{
    public Task<List<AntennaDto>> GetAntennasAsync() =>
        gatewayClient.GetAntennasAsync();

    public Task<AntennaDto> GetAntennaAsync(Guid id) =>
        gatewayClient.GetAntennaAsync(id);

    public async Task<AntennaDto> UpdateAntennaAsync(Guid id, UpdateAntennaDto dto)
    {
        dto.Status ??= "Off";

        if (dto.Sensibility.HasValue && (dto.Sensibility < 0 || dto.Sensibility > 100))
            throw new ArgumentOutOfRangeException(nameof(dto.Sensibility), "Sensibility must be between 0 and 100");

        if (dto.Power.HasValue && (dto.Power < 0 || dto.Power > 100))
            throw new ArgumentOutOfRangeException(nameof(dto.Power), "Power must be between 0 and 100");

        return await gatewayClient.UpdateAntennaAsync(id, dto);
    }
}
