using System.Threading.Tasks;

namespace Backend.Features.SystemConfig;

public interface ISystemService
{
    Task<SystemDto> GetSystemAsync();
}
