using System.Net.Http.Json;

namespace Backend.Features.Antennas;

public class GatewayException(int statusCode) : Exception($"Gateway returned status {statusCode}")
{
    public int StatusCode { get; } = statusCode;
}

public interface IGatewayClient
{
    Task<List<AntennaDto>> GetAntennasAsync();
    Task<AntennaDto> GetAntennaAsync(Guid id);
    Task<AntennaDto> UpdateAntennaAsync(Guid id, UpdateAntennaDto dto);
}

public class GatewayClient(HttpClient httpClient) : IGatewayClient
{
    public async Task<List<AntennaDto>> GetAntennasAsync()
    {
        using var response = await httpClient.GetAsync("antennas");
        if (!response.IsSuccessStatusCode)
            throw new GatewayException((int)response.StatusCode);
        return await response.Content.ReadFromJsonAsync<List<AntennaDto>>() ?? [];
    }

    public async Task<AntennaDto> GetAntennaAsync(Guid id)
    {
        using var response = await httpClient.GetAsync($"antennas/{id}");
        if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
            throw new KeyNotFoundException($"Antenna {id} not found");
        if (!response.IsSuccessStatusCode)
            throw new GatewayException((int)response.StatusCode);
        return await response.Content.ReadFromJsonAsync<AntennaDto>()
            ?? throw new GatewayException(500);
    }

    public async Task<AntennaDto> UpdateAntennaAsync(Guid id, UpdateAntennaDto dto)
    {
        using var response = await httpClient.PutAsJsonAsync($"antennas/{id}", dto);
        if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
            throw new KeyNotFoundException($"Antenna {id} not found");
        if (!response.IsSuccessStatusCode)
            throw new GatewayException((int)response.StatusCode);
        return await response.Content.ReadFromJsonAsync<AntennaDto>()
            ?? throw new GatewayException(500);
    }
}
