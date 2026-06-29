using System.Collections.Generic;
using System.Threading.Tasks;

namespace Backend.Features.SystemConfig;

public interface ISystemService
{
    Task<SystemDto> GetSystemAsync();
    Task<List<AntennaDto>> GetAntennasAsync();
    Task<AntennaDto> UpdateAntennaAsync(Guid antennaId, UpdateAntennaDto dto);
}
